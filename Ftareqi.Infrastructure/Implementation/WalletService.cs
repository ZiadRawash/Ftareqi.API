using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs;
using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.DTOs.Paymob.Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Mappers;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Enums.PaymentEnums;
using Ftareqi.Domain.Models;
using Ftareqi.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Ftareqi.Infrastructure.Implementation
{
	public class WalletService : IWalletService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<WalletService> _logger;
		private readonly IPaymentGateway _paymentGateway;
		private readonly INotificationOrchestrator _notificationOrchestrator;
		private readonly IDistributedCachingService _cache;
		private static readonly TimeSpan WalletCacheDuration = TimeSpan.FromMinutes(15);
		public WalletService(IUnitOfWork unitOfWork, ILogger<WalletService> logger, IPaymentGateway paymentGateway, INotificationOrchestrator notificationOrchestrator, IDistributedCachingService cache)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_paymentGateway = paymentGateway;
			_notificationOrchestrator = notificationOrchestrator;
			_cache = cache;
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
		public async Task<Result> LockAmountAsync(string userId, decimal amount)
		{
			if (string.IsNullOrWhiteSpace(userId))
				return Result.Failure("UserId is required");

			if (amount <= 0)
				return Result.Failure("Amount must be greater than zero");

			var wallet = await _unitOfWork.UserWallets.FirstOrDefaultAsync(x => x.UserId == userId);
			if (wallet == null)
				return Result.Failure("Wallet not found");

			if (wallet.Balance < amount)
				return Result.Failure("Insufficient available balance");

			await using var tx = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var balanceBefore = wallet.Balance;
				wallet.Balance -= amount;
				wallet.LockedBalance += amount;
				wallet.UpdatedAt = DateTime.UtcNow;

				var walletTransaction = new WalletTransaction
				{
					Type = TransactionType.locked,
					Amount = amount,
					BalanceBefore = balanceBefore,
					BalanceAfter = wallet.Balance,
					Status = TransactionStatus.Completed,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow,
					UserWalletId = wallet.Id,
				};

				_unitOfWork.UserWallets.Update(wallet);
				await _unitOfWork.WalletTransactions.AddAsync(walletTransaction);
				await _unitOfWork.SaveChangesAsync();
				await tx.CommitAsync();

				var metadata = new WalletTransactionMetadata
				{
					Preview = "Amount reserved in wallet",
					Amount = amount,
					Type = TransactionType.locked,
				};

				var notification = new NotificationInput(
					userId,
					NotificationCategory.Wallet,
					NotificationEventCode.AmountReserved,
					wallet.Id.ToString(),
					metadata);

				await _notificationOrchestrator.NotifyAsync(notification);
				await _cache.RemoveWalletCachesAsync(userId);

				_logger.LogInformation("Amount locked successfully for user {UserId}. Amount={Amount}, WalletId={WalletId}", userId, amount, wallet.Id);
				return Result.Success("Amount locked successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "LockAmountAsync failed for user {UserId}", userId);
				await tx.RollbackAsync();
				return Result.Failure("Failed to lock amount");
			}
		}
		public async Task<Result> ReleaseLockedAmountAsync(string userId, decimal amount)
		{
			if (string.IsNullOrWhiteSpace(userId))
				return Result.Failure("UserId is required");

			if (amount <= 0)
				return Result.Failure("Amount must be greater than zero");

			var wallet = await _unitOfWork.UserWallets.FirstOrDefaultAsync(x => x.UserId == userId);
			if (wallet == null)
				return Result.Failure("Wallet not found");

			if (wallet.LockedBalance < amount)
				return Result.Failure("Insufficient locked balance");

			await using var tx = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var balanceBefore = wallet.Balance;
				wallet.LockedBalance -= amount;
				wallet.Balance += amount;
				wallet.UpdatedAt = DateTime.UtcNow;
				var walletTransaction = new WalletTransaction
				{
					Type = TransactionType.Released,
					Amount = amount,
					BalanceBefore = balanceBefore,
					BalanceAfter = wallet.Balance,
					Status = TransactionStatus.Completed,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow,
					UserWalletId = wallet.Id,
				};
				_unitOfWork.UserWallets.Update(wallet);
				await _unitOfWork.WalletTransactions.AddAsync(walletTransaction);
				await _unitOfWork.SaveChangesAsync();
				await tx.CommitAsync();
				var metadata = new WalletTransactionMetadata
				{
					Preview = "Amount released to wallet",
					Amount = amount,
					Type = TransactionType.Refund,
				};
				var notification = new NotificationInput(
					userId,
					NotificationCategory.Wallet,
					NotificationEventCode.AmountReleased,
					wallet.Id.ToString(),
					metadata);

				await _notificationOrchestrator.NotifyAsync(notification);
				await _cache.RemoveWalletCachesAsync(userId);

				_logger.LogInformation("Locked amount released successfully for user {UserId}. Amount={Amount}, WalletId={WalletId}", userId, amount, wallet.Id);
				return Result.Success("Locked amount released successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ReleaseLockedAmountAsync failed for user {UserId}", userId);
				await tx.RollbackAsync();
				return Result.Failure("Failed to release locked amount");
			}
		}
		public async Task<Result<PaginatedResponse<TransactionDto>>> GetWalletTransactionsPaginated(string userId, GenericQueryReq queryReq)
		{
			if (string.IsNullOrEmpty(userId))
				return Result<PaginatedResponse<TransactionDto>>.Failure("No user is found");
			if (queryReq.IsWalletTransactionsFirstPage())
			{
				var cachedTransactions = await _cache.GetAsync<PaginatedResponse<TransactionDto>>(CacheKeys.WalletTransactionsFirstPage(userId));
				if (cachedTransactions != null)
				{
					_logger.LogInformation("Wallet transactions first page retrieved from cache for user {UserId}", userId);
					return Result<PaginatedResponse<TransactionDto>>.Success(cachedTransactions);
				}
			}
			var walletFound = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == userId);
			if (walletFound == null) 
				return Result<PaginatedResponse<TransactionDto>>.Failure("No wallet is found");
			var (walletTransactions, count) = await _unitOfWork.WalletTransactions.GetPagedAsync(
				queryReq.Page,
				queryReq.PageSize,
				x => x.CreatedAt,
				x => x.UserWalletId == walletFound.Id,
				true);
			var totalPages = (int)Math.Ceiling((double)count / queryReq.PageSize);
			var items = walletTransactions.Select(WalletMapper.ToTransactionDto).ToList();
			var response = new PaginatedResponse<TransactionDto>
			{
				Page = queryReq.Page,
				PageSize = queryReq.PageSize,
				TotalCount = count,
				TotalPages = totalPages,
				Items = items
			};
			if (queryReq.IsWalletTransactionsFirstPage())
			{
				await _cache.SetAsync(CacheKeys.WalletTransactionsFirstPage(userId), response, WalletCacheDuration);
			}
			return Result<PaginatedResponse<TransactionDto>>.Success(response);
		}
		public async Task<Result<WalletResDto>> GetWallet(string userId)
		{
			if (userId == null)
				return Result<WalletResDto>.Failure("No user is found");

			var cachedWallet = await _cache.GetAsync<WalletResDto>(CacheKeys.Wallet(userId));
			if (cachedWallet != null)
			{
				_logger.LogInformation("Wallet retrieved from cache for user {UserId}", userId);
				return Result<WalletResDto>.Success(cachedWallet);
			}
			var wallet = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == userId);

			if (wallet == null)
				return Result<WalletResDto>.Failure("No wallet is found");

			var response = new WalletResDto
			{
				Id = wallet.Id,
				Balance= wallet.Balance,
				CreatedAt = wallet.CreatedAt,
				IsLocked = wallet.IsLocked,
				LockedBalance = wallet.LockedBalance,
				UpdatedAt = wallet.UpdatedAt,
			};

			await _cache.SetAsync(CacheKeys.Wallet(userId), response, WalletCacheDuration);

			return Result<WalletResDto>.Success(response);
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
		/// Does NOT touch the wallet Balancer pending balance.
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

				// notification
				var metadata = new WalletTransactionMetadata
				{
					Preview = "Wallet charge failed",
					Amount = paymentTrnx.Amount,
					Type = walletTrnx.Type
				};
				var notification = new NotificationInput(
					paymentTrnx.UserId,
					NotificationCategory.Wallet,
					NotificationEventCode.WalletCharged, 
					walletTrnx.UserWalletId.ToString(),
					metadata);
				await _notificationOrchestrator.NotifyAsync(notification);
				await _cache.RemoveWalletCachesAsync(paymentTrnx.UserId);
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
		/// Credits the wallet Balance and marks both transactions as Completed/Success.
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

				walletTrnx.Status = TransactionStatus.Completed;
				walletTrnx.UpdatedAt = DateTime.UtcNow;
				walletTrnx.UserWallet.UpdatedAt = DateTime.UtcNow;
				walletTrnx.UserWallet.Balance+= paymentTrnx.Amount;

				_unitOfWork.WalletTransactions.Update(walletTrnx);
				_unitOfWork.PaymentTransactions.Update(paymentTrnx);
				_unitOfWork.UserWallets.Update(walletTrnx.UserWallet);

				await _unitOfWork.SaveChangesAsync();
				await tx.CommitAsync();

				// sending notification 
				var Metadata = new WalletTransactionMetadata
				{
					Preview = "Wallet Charged Successfully",
					Amount = paymentTrnx.Amount,
					Type = walletTrnx.Type,
				};
				var notification = new NotificationInput(
					paymentTrnx.UserId,
					NotificationCategory.Wallet,
					NotificationEventCode.WalletCharged,
					walletTrnx.UserWalletId.ToString(),
					Metadata);	
				await _notificationOrchestrator.NotifyAsync(notification);
				await _cache.RemoveWalletCachesAsync(paymentTrnx.UserId);
				
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

				await _cache.RemoveWalletCachesAsync(userId);

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