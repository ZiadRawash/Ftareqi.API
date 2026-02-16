using CloudinaryDotNet;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs;
using Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Mappers;
using Ftareqi.Domain.Enums.PaymentEnums;
using Ftareqi.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class WalletService : IWalletService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<WalletService> _logger;
		private readonly IPaymentGateway _paymentGateway;
		public WalletService(IUnitOfWork unitOfWork , ILogger<WalletService> logger , IPaymentGateway paymentGateway)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_paymentGateway = paymentGateway;
		}

		public async Task CreateWalletAsync(string userId)
		{
			var walletCreated = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == userId);
			if (walletCreated == null)
			{
				var CreateWallet = new UserWallet
				{
					UserId = userId,
					balance = 0,
					CreatedAt = DateTime.UtcNow,
					IsLocked = false,
					PendingBalance = 0,
				};
				await _unitOfWork.UserWallets.AddAsync(CreateWallet);
				await _unitOfWork.SaveChangesAsync();
			}
		}

		public async Task<Result<WalletTransactionDto>> GetWalletTransactions(int walletId)
		{
			var transactions = await _unitOfWork.WalletTransactions.FindAllAsNoTrackingAsync(x=>x.UserWalletId == walletId);
			var result = WalletMapper.ToDto(walletId, transactions ?? Enumerable.Empty<WalletTransaction>());
			return Result<WalletTransactionDto>.Success(result);

		}

		public async Task<Result<PaymentResponseDto>> TopUpWithCardAsync(string userId, decimal amount, Func<Task<PaymentInitiationResult>> initiateCardPayment)
		{
			if (initiateCardPayment == null) 
				throw new ArgumentNullException(nameof(initiateCardPayment));
			return await TopUpCoreAsync(userId, amount, initiateCardPayment,PaymentMethod.Card);	
		}

		public async Task<Result<PaymentResponseDto>> TopUpWithWalletAsync(string userId, decimal amount, Func<Task<PaymentInitiationResult>> initiateWalletPayment)
		{
			if (initiateWalletPayment == null)
				throw new ArgumentNullException(nameof(initiateWalletPayment));
			return await TopUpCoreAsync(userId, amount, initiateWalletPayment, PaymentMethod.MobileWallet);
		}
		private async Task<Result<PaymentResponseDto>> TopUpCoreAsync(string userId, decimal amount, Func<Task<PaymentInitiationResult>> initiatePayment, PaymentMethod method)
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
				_logger.LogWarning("Payment initiation failed for user {UserId}. Message: {Message}", userId, initiation?.Message);
				return Result<PaymentResponseDto>.Failure(initiation?.Message ?? "Payment initiation failed");
			}

			// Load user (no-tracking is fine for validation) and wallet (we will update it)
			var user = await _unitOfWork.Users.FirstOrDefaultAsNoTrackingAsync(x => x.Id == userId);
			if (user == null)
			{
				_logger.LogWarning("TopUp failed: User {UserId} not found", userId);
				return Result<PaymentResponseDto>.Failure("User not found");
			}

			// get wallet (tracking) - we need to update pending balance
			var wallet = await _unitOfWork.UserWallets.FirstOrDefaultAsync(x => x.UserId == userId);
			if (wallet == null)
			{
				_logger.LogWarning("TopUp failed: Wallet for user {UserId} not found", userId);
				return Result<PaymentResponseDto>.Failure("Wallet not found");
			}

			// Begin transaction to persist payment transaction and wallet transaction atomically
			await using var tx = await _unitOfWork.BeginTransactionAsync();
			try
			{
				// Create payment transaction record
				var paymentTrnx = new PaymentTransaction
				{
					Amount = amount,
					CreatedAt = DateTime.UtcNow,
					method = method,
					PaymentType = PaymentType.Credit,
					Reference = initiation.Reference,
					Status = PaymentStatus.Pending,
					UserId = userId
				};

				await _unitOfWork.PaymentTransactions.AddAsync(paymentTrnx);
				await _unitOfWork.SaveChangesAsync();

				// Create wallet transaction (pending)
				var walletTrnx = new WalletTransaction
				{
					Type = Domain.Enums.TransactionType.Deposit,
					Amount = amount,
					BalanceBefore = wallet.balance,
					BalanceAfter = wallet.balance + amount, // expected balance after successful completion
					Status = Domain.Enums.TransactionStatus.Pending,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow,
					UserWalletId = wallet.Id,
					PaymentTransactionId = paymentTrnx.Id,
				};

				await _unitOfWork.WalletTransactions.AddAsync(walletTrnx);

				// update wallet pending balance
				//wallet.PendingBalance += amount;
				//wallet.UpdatedAt = DateTime.UtcNow;
				//_unitOfWork.UserWallets.Update(wallet);

				await _unitOfWork.SaveChangesAsync();

				// commit transaction
				await tx.CommitAsync();

				_logger.LogInformation("TopUp initiated for user {UserId}. PaymentRef={Ref}, WalletTrxId={WalletTrxId}", userId, initiation.Reference, walletTrnx.Id);

				var response = new PaymentResponseDto
				{
					PaymentUrl = initiation.RedirectUrl ?? string.Empty,
					PaymobOrderId = initiation.PaymobOrderId,
					Reference = initiation.Reference,
					Status = initiation.Status ?? "pending"
				};

				return Result<PaymentResponseDto>.Success(response, "Payment initiated successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "TopUpCoreAsync failed for user {UserId}", userId);
				try
				{
					await tx.RollbackAsync();
				}
				catch (Exception rbEx)
				{
					_logger.LogError(rbEx, "Rollback failed after TopUpCoreAsync error for user {UserId}", userId);
				}

				return Result<PaymentResponseDto>.Failure("Failed to create payment records");
			}
		}
	}
}
