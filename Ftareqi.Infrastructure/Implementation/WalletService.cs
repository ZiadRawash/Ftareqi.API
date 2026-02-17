using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs;
using Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.DTOs.Paymob.Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Mappers;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Enums.PaymentEnums;
using Ftareqi.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Ftareqi.Infrastructure.Implementation
{
	public class WalletService : IWalletService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<WalletService> _logger;
		private readonly IPaymentGateway _paymentGateway;

		public WalletService(IUnitOfWork unitOfWork, ILogger<WalletService> logger, IPaymentGateway paymentGateway)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_paymentGateway = paymentGateway;
		}

		public async Task CreateWalletAsync(string userId)
		{
			var existing = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == userId);
			if (existing == null)
			{
				var wallet = new UserWallet
				{
					UserId = userId,
					Balance= 0,
					CreatedAt = DateTime.UtcNow,
					IsLocked = false,
					LockedBalance = 0,
				};
				await _unitOfWork.UserWallets.AddAsync(wallet);
				await _unitOfWork.SaveChangesAsync();
			}
		}

		public async Task<Result<WalletTransactionDto>> GetWalletTransactions(string userId)
		{
			if(userId == null)
				return Result<WalletTransactionDto>.Failure("No user is found");
			var wallet = await _unitOfWork.UserWallets.FirstOrDefaultAsync(x => x.UserId == userId, x => x.WalletTransactions);

			if (wallet == null) 
				return Result<WalletTransactionDto>.Failure("No wallet is found");

			var result = WalletMapper.ToDto(wallet!.Id, wallet.WalletTransactions ?? Enumerable.Empty<WalletTransaction>());
			return Result<WalletTransactionDto>.Success(result);
		}

		public async Task<Result<WalletResDto>> GetWallet(string userId)
		{
			if (userId == null)
				return Result<WalletResDto>.Failure("No user is found");
			var wallet = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == userId);

			if (wallet == null)
				return Result<WalletResDto>.Failure("No wallet is found");

			return Result<WalletResDto>.Success(new WalletResDto
			{
				Id = wallet.Id,
				Balance= wallet.Balance,
				CreatedAt = wallet.CreatedAt,
				IsLocked = wallet.IsLocked,
				LockedBalance = wallet.LockedBalance,
				UpdatedAt = wallet.UpdatedAt,
			});
		}

		public async Task<Result<PaymentResponseDto>> TopUpWithCardAsync(
			string userId, decimal amount, Func<Task<PaymentInitiationResult>> initiateCardPayment)
		{
			if (initiateCardPayment == null)
				throw new ArgumentNullException(nameof(initiateCardPayment));

			return await TopUpCoreAsync(userId, amount, initiateCardPayment, PaymentMethod.Card);
		}

		public async Task<Result<PaymentResponseDto>> TopUpWithWalletAsync(
			string userId, decimal amount, Func<Task<PaymentInitiationResult>> initiateWalletPayment)
		{
			if (initiateWalletPayment == null)
				throw new ArgumentNullException(nameof(initiateWalletPayment));

			return await TopUpCoreAsync(userId, amount, initiateWalletPayment, PaymentMethod.MobileWallet);
		}

		/// <summary>
		/// Entry point for Paymob's server-side callback (transaction.processed webhook or redirect).
		///
		/// Two outcomes from the gateway:
		///   Failure → HMAC_INVALID: request is tampered/corrupt → log and stop, do NOT touch any records
		///   Success → dto.PaymentSucceeded drives whether we credit or mark failed
		/// </summary>
		public async Task ProcessPaymentCallBack(string hmac, PaymobCallbackDto callback)
		{
			var callbackResult = _paymentGateway.Callback(hmac, callback);

			if (callbackResult.IsFailure)
			{
				_logger.LogWarning("Callback rejected: HMAC invalid or empty payload. No records updated.");
				return;
			}


			if (callbackResult.Data!.PaymentSucceeded)
				await HandleSuccessfulPaymentAsync(callbackResult.Data);
			else
				await HandleFailedPaymentAsync(callbackResult.Data);
		}

		/// <summary>
		/// Marks both PaymentTransaction and WalletTransaction as Failed.
		/// Does NOT touch the wallet Balanceor pending balance.
		/// </summary>
		private async Task HandleFailedPaymentAsync(PaymentCallbackResultDto? dto)
		{
			if (dto == null)
			{
				_logger.LogError("HandleFailedPaymentAsync called with null dto — cannot update records.");
				return;
			}

			_logger.LogInformation(
				"Handling failed payment. MerchantId (Reference): {MerchantId}", dto.MerchantId);

			await using var tx = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var paymentTrnx = await _unitOfWork.PaymentTransactions
					.FirstOrDefaultAsync(x => x.Reference == dto.MerchantId);

				if (paymentTrnx == null)
				{
					_logger.LogWarning(
						"HandleFailedPaymentAsync: PaymentTransaction not found for Reference={Reference}",
						dto.MerchantId);
					await tx.RollbackAsync();
					return;
				}

				// Map enum explicitly — never rely on implicit string-to-enum conversion
				paymentTrnx.Status = PaymentStatus.Failed;
				paymentTrnx.UpdatedAt= DateTime.UtcNow;
				var walletTrnx = await _unitOfWork.WalletTransactions
					.FirstOrDefaultAsync(
						x => x.PaymentTransactionId == paymentTrnx.Id,
						x => x.UserWallet);

				if (walletTrnx == null)
				{
					_logger.LogWarning(
						"HandleFailedPaymentAsync: WalletTransaction not found for PaymentTransactionId={Id}",
						paymentTrnx.Id);
					_unitOfWork.PaymentTransactions.Update(paymentTrnx);
					await _unitOfWork.SaveChangesAsync();
					await tx.CommitAsync();
					return;
				}

				walletTrnx.Status = TransactionStatus.Failed;
				walletTrnx.UpdatedAt = DateTime.UtcNow;

				_unitOfWork.PaymentTransactions.Update(paymentTrnx);
				_unitOfWork.WalletTransactions.Update(walletTrnx);

				await _unitOfWork.SaveChangesAsync();
				await tx.CommitAsync();

				_logger.LogInformation(
					"Records marked as Failed. PaymentTrnxId={PaymentId}, WalletTrnxId={WalletId}",
					paymentTrnx.Id, walletTrnx.Id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "HandleFailedPaymentAsync threw — rolling back.");
				await tx.RollbackAsync();
			}
		}

		/// <summary>
		/// Credits the wallet Balanceand marks both transactions as Completed/Success.
		/// </summary>
		private async Task HandleSuccessfulPaymentAsync(PaymentCallbackResultDto dto)
		{
			_logger.LogInformation(
				"Handling successful payment. MerchantId (Reference): {MerchantId}", dto.MerchantId);

			await using var tx = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var paymentTrnx = await _unitOfWork.PaymentTransactions
					.FirstOrDefaultAsync(x => x.Reference == dto.MerchantId);

				if (paymentTrnx == null)
				{
					_logger.LogWarning(
						"HandleSuccessfulPaymentAsync: PaymentTransaction not found for Reference={Reference}",
						dto.MerchantId);
					await tx.RollbackAsync();
					return;
				}

				if (paymentTrnx.Status == PaymentStatus.Success)
				{
					_logger.LogWarning(
						"Duplicate callback received for already-succeeded PaymentTransaction {Id} — ignoring.",
						paymentTrnx.Id);
					await tx.RollbackAsync();
					return;
				}

				// Map enum explicitly
				paymentTrnx.Status = PaymentStatus.Success;

				var walletTrnx = await _unitOfWork.WalletTransactions
					.FirstOrDefaultAsync(
						x => x.PaymentTransactionId == paymentTrnx.Id,
						x => x.UserWallet);

				if (walletTrnx == null)
				{
					_logger.LogWarning(
						"HandleSuccessfulPaymentAsync: WalletTransaction not found for PaymentTransactionId={Id}",
						paymentTrnx.Id);
					await tx.RollbackAsync();
					return;
				}

				// Map enum explicitly
				walletTrnx.Status = TransactionStatus.Completed;
				walletTrnx.UpdatedAt = DateTime.UtcNow;

				walletTrnx.UserWallet.UpdatedAt = DateTime.UtcNow;

				walletTrnx.UserWallet.Balance+= paymentTrnx.Amount;

				_unitOfWork.WalletTransactions.Update(walletTrnx);
				_unitOfWork.PaymentTransactions.Update(paymentTrnx);
				_unitOfWork.UserWallets.Update(walletTrnx.UserWallet);

				await _unitOfWork.SaveChangesAsync();
				await tx.CommitAsync();

				_logger.LogInformation(
					"Wallet credited. UserId={UserId}, Amount={Amount}, NewBalance={Balance}",
					walletTrnx.UserWallet.UserId,
					paymentTrnx.Amount,
					walletTrnx.UserWallet.Balance);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "HandleSuccessfulPaymentAsync threw — rolling back.");
				await tx.RollbackAsync();
			}
		}

		private async Task<Result<PaymentResponseDto>> TopUpCoreAsync(
			string userId, decimal amount,
			Func<Task<PaymentInitiationResult>> initiatePayment,
			PaymentMethod method)
		{
			if (string.IsNullOrWhiteSpace(userId))
				return Result<PaymentResponseDto>.Failure("UserId is required");

			if (amount <= 0)
				return Result<PaymentResponseDto>.Failure("Amount must be greater than zero");

			PaymentInitiationResult initiation;
			try
			{
				initiation = await initiatePayment();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error while calling initiatePayment for user {UserId}", userId);
				return Result<PaymentResponseDto>.Failure("Failed to initiate payment");
			}

			if (initiation == null || !initiation.Success)
			{
				_logger.LogWarning("Payment initiation failed for user {UserId}. Message: {Message}",
					userId, initiation?.Message);
				return Result<PaymentResponseDto>.Failure(initiation?.Message ?? "Payment initiation failed");
			}

			var user = await _unitOfWork.Users.FirstOrDefaultAsNoTrackingAsync(x => x.Id == userId);
			if (user == null)
			{
				_logger.LogWarning("TopUp failed: User {UserId} not found", userId);
				return Result<PaymentResponseDto>.Failure("User not found");
			}

			var wallet = await _unitOfWork.UserWallets.FirstOrDefaultAsync(x => x.UserId == userId);
			if (wallet == null)
			{
				_logger.LogWarning("TopUp failed: Wallet for user {UserId} not found", userId);
				return Result<PaymentResponseDto>.Failure("Wallet not found");
			}
			await using var tx = await _unitOfWork.BeginTransactionAsync();
			try
			{

				var paymentTrnx = new PaymentTransaction
				{
					Amount = amount,
					CreatedAt = DateTime.UtcNow,
					Method = method,                   
					PaymentType = PaymentType.Credit,   
					Reference = initiation.Reference,
					Status = PaymentStatus.Pending,     
					UserId = userId,
					UpdatedAt = DateTime.UtcNow,
					
				};

				await _unitOfWork.PaymentTransactions.AddAsync(paymentTrnx);
				await _unitOfWork.SaveChangesAsync();

				var walletTrnx = new WalletTransaction
				{
					Type = TransactionType.Deposit,         
					Amount = amount,
					BalanceBefore = wallet.Balance,
					BalanceAfter = wallet.Balance+ amount,
					Status = TransactionStatus.Pending,     
					CreatedAt = DateTime.UtcNow,
					UserWalletId = wallet.Id,
					PaymentTransactionId = paymentTrnx.Id,
				};

				await _unitOfWork.WalletTransactions.AddAsync(walletTrnx);
				await _unitOfWork.SaveChangesAsync();
				await tx.CommitAsync();

				_logger.LogInformation(
					"TopUp initiated for user {UserId}. PaymentRef={Ref}, WalletTrxId={WalletTrxId}",
					userId, initiation.Reference, walletTrnx.Id);

				var response = new PaymentResponseDto
				{
					PaymentUrl = initiation.RedirectUrl ?? string.Empty,
				};

				return Result<PaymentResponseDto>.Success(response, "Payment initiated successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "TopUpCoreAsync failed for user {UserId}", userId);
				try { await tx.RollbackAsync(); }
				catch (Exception rbEx)
				{
					_logger.LogError(rbEx, "Rollback failed after TopUpCoreAsync error for user {UserId}", userId);
				}

				return Result<PaymentResponseDto>.Failure("Failed to create payment records");
			}
		}
	}
}