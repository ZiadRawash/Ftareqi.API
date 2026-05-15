using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.BackgroundJobs
{
	public class BookingJobs : IBookingJobs
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IBookingService _bookingService;
		private readonly IWalletService _walletService;
		private readonly ILogger<BookingJobs> _logger;

		public BookingJobs(
			IUnitOfWork unitOfWork,
			IBookingService bookingService,
			IWalletService walletService,
			ILogger<BookingJobs> logger)
		{
			_unitOfWork = unitOfWork;
			_bookingService = bookingService;
			_walletService = walletService;
			_logger = logger;
		}

		public async Task ExpireBookingAsync(int bookingId)
		{
			await using var tx = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
					x => x.Id == bookingId && !x.IsDeleted,
					x => x.Ride);

				if (booking == null)
				{
					_logger.LogWarning("ExpireBookingAsync: booking {BookingId} not found", bookingId);
					await tx.RollbackAsync();
					return;
				}

				if (booking.Status != BookingStatus.Pending)
				{
					_logger.LogInformation("ExpireBookingAsync: booking {BookingId} has status {Status}, skipping", bookingId, booking.Status);
					await tx.RollbackAsync();
					return;
				}

				var expireResult = await _bookingService.ExpireBooking(bookingId);
				if (expireResult.IsFailure)
				{
					_logger.LogError("ExpireBookingAsync: failed to expire booking {BookingId}. Error: {Error}", bookingId, expireResult.Message);
					await tx.RollbackAsync();
					return;
				}

				var lockTransactions = await _unitOfWork.WalletTransactions.FindAllAsTrackingAsync(
					x => x.RideBookingId == bookingId && x.Type == TransactionType.locked,
					x => x.UserWallet);

				var totalLockedAmount = lockTransactions.Sum(x => x.Amount);
				if (totalLockedAmount <= 0)
				{
					await _unitOfWork.SaveChangesAsync();
					await tx.CommitAsync();
					_logger.LogInformation("ExpireBookingAsync: booking {BookingId} expired with no locked amount to release", bookingId);
					return;
				}

				var walletUserId = lockTransactions
					.Select(x => x.UserWallet?.UserId)
					.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
				if (string.IsNullOrWhiteSpace(walletUserId))
				{
					_logger.LogError("ExpireBookingAsync: failed to resolve wallet user for booking {BookingId}", bookingId);
					await tx.RollbackAsync();
					return;
				}

				var releaseResult = await _walletService.ReleaseLockedAmountAsync(walletUserId, totalLockedAmount);
				if (releaseResult.IsFailure)
				{
					_logger.LogWarning("ExpireBookingAsync: failed to release locked amount for booking {BookingId}. Error: {Error}", bookingId, releaseResult.Message);
					await tx.RollbackAsync();
					return;
				}

				await _unitOfWork.SaveChangesAsync();
				await tx.CommitAsync();

				_logger.LogInformation("ExpireBookingAsync: successfully expired booking {BookingId} and released {Amount} for user {UserId}", bookingId, totalLockedAmount, walletUserId);
			}
			catch (Exception ex)
			{
				await tx.RollbackAsync();
				_logger.LogError(ex, "ExpireBookingAsync: failed during processing for booking {BookingId}", bookingId);
			}
		}


	}
}