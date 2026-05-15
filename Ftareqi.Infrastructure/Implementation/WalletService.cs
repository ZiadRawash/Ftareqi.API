using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs;
using Ftareqi.Application.DTOs.Paymob;
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
		private readonly IDistributedCachingService _cache;
		private static readonly TimeSpan WalletCacheDuration = TimeSpan.FromMinutes(15);

		public WalletService(IUnitOfWork unitOfWork, ILogger<WalletService> logger, IDistributedCachingService cache)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_cache = cache;
		}

		public async Task CreateWalletAsync(string userId)
		{
			var existing = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == userId);
			if (existing != null)
				return;

			var wallet = new UserWallet
			{
				UserId = userId,
				Balance = 0,
				CreatedAt = DateTime.UtcNow,
				IsLocked = false,
				LockedBalance = 0,
			};

			await _unitOfWork.UserWallets.AddAsync(wallet);
			await _unitOfWork.SaveChangesAsync();
		}

		public async Task<Result<WalletResDto>> GetWallet(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId))
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
				Balance = wallet.Balance,
				CreatedAt = wallet.CreatedAt,
				IsLocked = wallet.IsLocked,
				LockedBalance = wallet.LockedBalance,
				UpdatedAt = wallet.UpdatedAt,
			};

			await _cache.SetAsync(CacheKeys.Wallet(userId), response, WalletCacheDuration);
			return Result<WalletResDto>.Success(response);
		}

		public async Task<Result<PaginatedResponse<TransactionDto>>> GetWalletTransactionsPaginated(string userId, GenericQueryReq queryReq)
		{
			if (string.IsNullOrWhiteSpace(userId))
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
				await _cache.SetAsync(CacheKeys.WalletTransactionsFirstPage(userId), response, WalletCacheDuration);

			return Result<PaginatedResponse<TransactionDto>>.Success(response);
		}

		public async Task<Result<WalletTransaction>> LockAmountAsync(string userId, decimal amount)
		{
			if (string.IsNullOrWhiteSpace(userId))
				return Result<WalletTransaction>.Failure("UserId is required");

			if (amount <= 0)
				return Result<WalletTransaction>.Failure("Amount must be greater than zero");

			var wallet = await _unitOfWork.UserWallets.FirstOrDefaultAsync(x => x.UserId == userId);
			if (wallet == null)
				return Result<WalletTransaction>.Failure("Wallet not found");

			if (wallet.Balance < amount)
				return Result<WalletTransaction>.Failure("Insufficient available balance");

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
				await _cache.RemoveWalletCachesAsync(userId);

				_logger.LogInformation("Lock prepared for user {UserId}. Amount={Amount}, WalletId={WalletId}", userId, amount, wallet.Id);
				return Result<WalletTransaction>.Success(walletTransaction, "Lock prepared successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "LockAmountAsync failed for user {UserId}", userId);
				return Result<WalletTransaction>.Failure("Failed to prepare lock");
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
				await _cache.RemoveWalletCachesAsync(userId);

				_logger.LogInformation("Release prepared for user {UserId}. Amount={Amount}, WalletId={WalletId}", userId, amount, wallet.Id);
				return Result.Success("Release prepared successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ReleaseLockedAmountAsync failed for user {UserId}", userId);
				return Result.Failure("Failed to prepare release");
			}
		}

		public async Task<Result<PaymentResponseDto>> RecordPendingTopUpAsync(string userId, decimal amount, PaymentMethod method, string reference)
		{
			if (string.IsNullOrWhiteSpace(userId))
				return Result<PaymentResponseDto>.Failure("UserId is required");

			if (amount <= 0)
				return Result<PaymentResponseDto>.Failure("Amount must be greater than zero");

			if (string.IsNullOrWhiteSpace(reference))
				return Result<PaymentResponseDto>.Failure("Reference is required");

			var wallet = await _unitOfWork.UserWallets.FirstOrDefaultAsync(x => x.UserId == userId);
			if (wallet == null)
				return Result<PaymentResponseDto>.Failure("Wallet not found");

			var user = await _unitOfWork.Users.FirstOrDefaultAsNoTrackingAsync(x => x.Id == userId);
			if (user == null)
				return Result<PaymentResponseDto>.Failure("User not found");

			try
			{
				var now = DateTime.UtcNow;
				var paymentTrnx = new PaymentTransaction
				{
					Amount = amount,
					CreatedAt = now,
					Method = method,
					PaymentType = PaymentType.Credit,
					Reference = reference,
					Status = PaymentStatus.Pending,
					UpdatedAt = now,
					UserId = userId,
				};

				var walletTrnx = new WalletTransaction
				{
					Type = TransactionType.Deposit,
					Amount = amount,
					BalanceBefore = wallet.Balance,
					BalanceAfter = wallet.Balance + amount,
					Status = TransactionStatus.Pending,
					CreatedAt = now,
					UpdatedAt = now,
					UserWalletId = wallet.Id,
					PaymentTransaction = paymentTrnx,
				};

				await _unitOfWork.PaymentTransactions.AddAsync(paymentTrnx);
				await _unitOfWork.WalletTransactions.AddAsync(walletTrnx);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("TopUp pending records created for user {UserId}. Reference={Reference}, WalletTrxId={WalletTrxId}", userId, reference, walletTrnx.Id);
				return Result<PaymentResponseDto>.Success(new PaymentResponseDto(), "Payment record created successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "RecordPendingTopUpAsync failed for user {UserId}", userId);
				return Result<PaymentResponseDto>.Failure("Failed to create payment records");
			}
		}

		public async Task<Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)>> CreditWalletAsync(string merchantReference)
		{
			return await UpdateWalletTransactionStatusAsync(merchantReference, PaymentStatus.Success, TransactionStatus.Completed, true);
		}

		public async Task<Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)>> FailWalletTransactionAsync(string merchantReference)
		{
			return await UpdateWalletTransactionStatusAsync(merchantReference, PaymentStatus.Failed, TransactionStatus.Failed, false);
		}

		private async Task<Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)>> UpdateWalletTransactionStatusAsync(
			string merchantReference,
			PaymentStatus targetPaymentStatus,
			TransactionStatus targetWalletStatus,
			bool creditBalance)
		{
			if (string.IsNullOrWhiteSpace(merchantReference))
				return Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)>.Failure("Reference is required");

			var paymentTrnx = await _unitOfWork.PaymentTransactions.FirstOrDefaultAsync(x => x.Reference == merchantReference);
			if (paymentTrnx == null)
				return Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)>.Failure("Payment transaction not found");

			var walletTrnx = await _unitOfWork.WalletTransactions.FirstOrDefaultAsync(x => x.PaymentTransactionId == paymentTrnx.Id, x => x.UserWallet);
			if (walletTrnx == null)
				return Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)>.Failure("Wallet transaction not found");

			if (paymentTrnx.Status != PaymentStatus.Pending)
				return Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)>.Failure("Payment transaction is not pending");

			try
			{
				var now = DateTime.UtcNow;
				paymentTrnx.Status = targetPaymentStatus;
				paymentTrnx.UpdatedAt = now;
				walletTrnx.Status = targetWalletStatus;
				walletTrnx.UpdatedAt = now;

				if (creditBalance)
				{
					walletTrnx.UserWallet.Balance += paymentTrnx.Amount;
					walletTrnx.UserWallet.UpdatedAt = now;
					_unitOfWork.UserWallets.Update(walletTrnx.UserWallet);
				}

				_unitOfWork.PaymentTransactions.Update(paymentTrnx);
				_unitOfWork.WalletTransactions.Update(walletTrnx);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Wallet transaction updated for Reference={Reference}. PaymentStatus={PaymentStatus}, WalletStatus={WalletStatus}", merchantReference, paymentTrnx.Status, walletTrnx.Status);
				return Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)>.Success((walletTrnx.UserWallet.UserId, walletTrnx, paymentTrnx), creditBalance ? "Wallet credited successfully" : "Wallet transaction failed");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Wallet transaction update failed for Reference={Reference}", merchantReference);
				return Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)>.Failure("Failed to update wallet transaction");
			}
		}
	}
}
