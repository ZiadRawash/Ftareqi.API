using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Bookings;
using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Ftareqi.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Orchestrators
{
	public class RideOrchestrator : IRideOrchestrator
	{
		private readonly ILogger<RideOrchestrator> _logger;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IRideService _rideService;
		private readonly IWalletService _walletService;
		private readonly IDistributedCachingService _cache;
		private readonly IBookingService _bookingService;
		private readonly INotificationOrchestrator _notificationOrchestrator;
		private const int ExpiredBookingsBatchSize = 200;
		public RideOrchestrator(ILogger<RideOrchestrator> logger, IUnitOfWork unitOfWork,
			IRideService rideService, IWalletService walletService,
			 IDistributedCachingService cache,
			 IBookingService bookingService,
			 INotificationOrchestrator notificationOrchestrator
			 )
		{

			_logger = logger;
			_unitOfWork = unitOfWork;
			_rideService = rideService;
			_walletService = walletService;
			_cache = cache;
			_bookingService = bookingService;
			_notificationOrchestrator = notificationOrchestrator;

		}
		// create rideBookingRequest
		//1-validateMoney 2- validate the bookingParam against the ride 3- look money 3- create request 4-send notifications of looking money and the request for driver
		public async Task<Result> CreateRideBookingRequest(CreateBookingRequestDto model, string userId)
		{
			var userFound = await _unitOfWork.Users.GetByIdAsync(userId);
			if (userFound == null)
			{
				_logger.LogError("user with {id} not found", userId);
				return Result.Failure("User not found");
			}
			var rideFound = await _unitOfWork.Rides.FirstOrDefaultAsNoTrackingAsync(x => x.Id == model.RideId);
			if (rideFound == null)
			{
				_logger.LogError("no such ride with id :{id}", model.RideId);
				return Result.Failure("ride not found");
			}
			var driverProfile = await _unitOfWork.DriverProfiles.FirstOrDefaultAsNoTrackingAsync(x => x.Id == rideFound.DriverProfileId);
			if (driverProfile == null)
			{
				_logger.LogError("no driver profile found for ride id :{id}", model.RideId);
				return Result.Failure("driver not found");
			}
			var totalMoney = rideFound.PricePerSeat * model.NumberOfSeats;

			await using var tx = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var moneyLocked = await _walletService.LockAmountAsync(userId, totalMoney);
				if (moneyLocked.IsFailure || moneyLocked.Data == null)
				{
					await tx.RollbackAsync();
					return Result.Failure(moneyLocked.Message);
				}

				var reservedBooking = await _bookingService.CreateBooking(model, userId);
				if (reservedBooking.IsFailure || reservedBooking.Data == null)
				{
					await tx.RollbackAsync();
					return Result.Failure(reservedBooking.Message);
				}

				moneyLocked.Data.RideBooking = reservedBooking.Data;
				moneyLocked.Data.UpdatedAt = DateTime.UtcNow;

				await _unitOfWork.SaveChangesAsync();
				await tx.CommitAsync();

				await SendWalletAmountReservedNotification(userId, moneyLocked.Data.UserWalletId, totalMoney);
				await _cache.RemoveWalletCachesAsync(userId);
				await SendRequestNotification(driverProfile.UserId, reservedBooking.Data.Id);

				return Result.Success("Booking Created successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CreateRideBookingRequest failed for user {UserId}", userId);
				await tx.RollbackAsync();
				return Result.Failure("Unexpected error happened while creating booking request");
			}
		}
		public async Task<Result> AcceptRideBookingRequest(int bookingId, string driverId)
		{
			var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
				x => x.Id == bookingId && !x.IsDeleted,
				x => x.Ride,
				x => x.Ride!.DriverProfile);
			if (booking == null)
			{
				return Result.Failure("Booking not found");
			}
			var rideAccepted= await _bookingService.AcceptBooking(bookingId, driverId);
			if (rideAccepted.IsFailure)
			{
				_logger.LogError("Error Happened :{error}", rideAccepted.Message);
				return Result.Failure(rideAccepted.Message);
			}
			await SendAcceptNotification(booking!.UserId, bookingId);
			return Result.Success("Booking Accepted successfully");
		}
		public async Task<Result> DeclineRideBookingRequest(int bookingId, string driverId)
		{
			if (string.IsNullOrWhiteSpace(driverId))
			{
				return Result.Failure("Driver user id is required");
			}

			var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
				x => x.Id == bookingId && !x.IsDeleted,
				x => x.Ride,
				x => x.Ride!.DriverProfile);

			if (booking == null)
			{
				return Result.Failure("Booking not found");
			}

			if (booking.Ride == null || booking.Ride.DriverProfile == null)
			{
				return Result.Failure("Ride not found for this booking");
			}

			if (booking.Ride.DriverProfile.UserId != driverId)
			{
				return Result.Failure("You are not authorized to decline this booking");
			}

			if (booking.Status != BookingStatus.Pending)
			{
				return Result.Failure("Only pending bookings can be declined");
			}

			await using var tx = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var cancelResult = await _bookingService.CancelBooking(bookingId, BookingCancellationType.Driver);
				if (cancelResult.IsFailure)
				{
					await tx.RollbackAsync();
					return Result.Failure(cancelResult.Message);
				}

				var releaseResult = await ReleaseLockedAmountForBooking(bookingId, booking.UserId);
				if (releaseResult.IsFailure)
				{
					_logger.LogError("DeclineRideBookingRequest: booking {BookingId} declined but failed to release locked amount for user {UserId}. Error: {Error}", bookingId, booking.UserId, releaseResult.Message);
					await tx.RollbackAsync();
					return Result.Failure($"Booking declined but failed to release locked amount. {releaseResult.Message}");
				}

				await _unitOfWork.SaveChangesAsync();
				await tx.CommitAsync();

				if (releaseResult.Data > 0)
				{
					var bookingWallet = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == booking.UserId);
					if (bookingWallet != null)
					{
						await SendWalletAmountReleasedNotification(booking.UserId, bookingWallet.Id, releaseResult.Data);
						await _cache.RemoveWalletCachesAsync(booking.UserId);
					}
				}

				await SendDeclineNotification(booking.UserId, bookingId);
				return Result.Success("Booking declined successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "DeclineRideBookingRequest failed for booking {BookingId}", bookingId);
				await tx.RollbackAsync();
				return Result.Failure("Unexpected error happened while declining booking");
			}
		}
		public async Task<Result> CancelRideBookingByRider(int bookingId, string riderId)
		{
			if (string.IsNullOrWhiteSpace(riderId))
			{
				return Result.Failure("Rider user id is required");
			}

			var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
				x => x.Id == bookingId && !x.IsDeleted,
				x => x.Ride,
				x => x.Ride!.DriverProfile);

			if (booking == null)
			{
				return Result.Failure("Booking not found");
			}

			if (!string.Equals(booking.UserId, riderId, StringComparison.Ordinal))
			{
				return Result.Failure("You are not authorized to cancel this booking");
			}

			if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Accepted)
			{
				return Result.Failure("Only pending or accepted bookings can be cancelled by rider");
			}

			await using var tx = await _unitOfWork.BeginTransactionAsync();
			try
			{
				var cancelResult = await _bookingService.CancelBooking(bookingId, BookingCancellationType.Rider);
				if (cancelResult.IsFailure)
				{
					await tx.RollbackAsync();
					return Result.Failure(cancelResult.Message);
				}

				var releaseResult = await ReleaseLockedAmountForBooking(bookingId, riderId);
				if (releaseResult.IsFailure)
				{
					_logger.LogError("CancelRideBookingByRider: booking {BookingId} cancelled by rider but failed to release locked amount for user {UserId}. Error: {Error}", bookingId, riderId, releaseResult.Message);
					await tx.RollbackAsync();
					return Result.Failure($"Booking cancelled but failed to release locked amount. {releaseResult.Message}");
				}

				await _unitOfWork.SaveChangesAsync();
				await tx.CommitAsync();

				if (releaseResult.Data > 0)
				{
					var riderWallet = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == riderId);
					if (riderWallet != null)
					{
						await SendWalletAmountReleasedNotification(riderId, riderWallet.Id, releaseResult.Data);
						await _cache.RemoveWalletCachesAsync(riderId);
					}
				}

				return Result.Success("Booking cancelled successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CancelRideBookingByRider failed for booking {BookingId}", bookingId);
				await tx.RollbackAsync();
				return Result.Failure("Unexpected error happened while cancelling booking");
			}
		}
		public async Task<Result> HandleExpiredBookings()
		{
			var now = DateTime.UtcNow;
			var bookings = await _unitOfWork.RideBookings.FindAllAsTrackingAsync(
				x => x.ExpiresAt <= now && x.Status == BookingStatus.Pending);
			if (!bookings.Any())
			{
				_logger.LogInformation("HandleExpiredBookings: no pending bookings to expire at {Now}", now);
				return Result.Success("No expired bookings to process");
			}

			int successCount = 0;
			int failureCount = 0;
			foreach (var booking in bookings)
			{
				await using var tx = await _unitOfWork.BeginTransactionAsync();
				try
				{
					var expireResult = await _bookingService.ExpireBooking(booking.Id);
					if (expireResult.IsFailure)
					{
						await tx.RollbackAsync();
						_logger.LogError("HandleExpiredBookings: failed to expire booking {BookingId}. Error: {Error}", booking.Id, expireResult.Message);
						failureCount++;
						continue;
					}

					var lockTransactions = await _unitOfWork.WalletTransactions.FindAllAsTrackingAsync(
						x => x.RideBookingId == booking.Id && x.Type == TransactionType.locked,
						x => x.UserWallet);

					var totalLockedAmount = lockTransactions.Sum(x => x.Amount);
					if (totalLockedAmount <= 0)
					{
						await _unitOfWork.SaveChangesAsync();
						await tx.CommitAsync();
						_logger.LogInformation("HandleExpiredBookings: booking {BookingId} expired with no locked amount to release", booking.Id);
						successCount++;
						continue;
					}

					var walletUserId = lockTransactions
						.Select(x => x.UserWallet?.UserId)
						.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
					if (string.IsNullOrWhiteSpace(walletUserId))
					{
						await tx.RollbackAsync();
						_logger.LogError("HandleExpiredBookings: failed to resolve wallet user for booking {BookingId}", booking.Id);
						failureCount++;
						continue;
					}

					var releaseResult = await _walletService.ReleaseLockedAmountAsync(walletUserId, totalLockedAmount);
					if (releaseResult.IsFailure)
					{
						await tx.RollbackAsync();
						_logger.LogWarning("HandleExpiredBookings: failed to release locked amount for booking {BookingId}. Error: {Error}", booking.Id, releaseResult.Message);
						failureCount++;
						continue;
					}

					await _unitOfWork.SaveChangesAsync();
					await tx.CommitAsync();

					var wallet = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == walletUserId);
					if (wallet != null)
					{
						await SendWalletAmountReleasedNotification(walletUserId, wallet.Id, totalLockedAmount);
						await _cache.RemoveWalletCachesAsync(walletUserId);
					}

					_logger.LogInformation("HandleExpiredBookings: successfully expired booking {BookingId} and released {Amount} for user {UserId}", booking.Id, totalLockedAmount, walletUserId);
					successCount++;
				}
				catch (Exception ex)
				{
					failureCount++;
					_logger.LogError(ex, "HandleExpiredBookings: failed during processing for booking {BookingId}", booking.Id);
					await tx.RollbackAsync();
				}
			}

			if (failureCount == 0)
			{
				return Result.Success($"Successfully processed {successCount} expired bookings");
			}

			return Result.Failure($"Processed {successCount} expired bookings with {failureCount} failures");
		}
		public async Task<Result> CancelRide(int rideId, string driverId)
		{
			if (rideId <= 0)
			{
				return Result.Failure("Valid ride id is required");
			}

			if (string.IsNullOrWhiteSpace(driverId))
			{
				return Result.Failure("Driver user id is required");
			}

			var rideFound = await _unitOfWork.Rides.FirstOrDefaultAsync(
				x => x.Id == rideId,
				x => x.DriverProfile,
				x => x.RideBookings);

			if (rideFound == null)
			{
				return Result.Failure("Ride not found");
			}

			if (rideFound.DriverProfile == null)
			{
				return Result.Failure("Driver profile not found for this ride");
			}

			if (!string.Equals(rideFound.DriverProfile.UserId, driverId, StringComparison.Ordinal))
			{
				return Result.Failure("You are not authorized to cancel this ride");
			}

			if (rideFound.Status != RideStatus.Scheduled)
			{
				return Result.Failure("Only scheduled rides can be cancelled");
			}

			var cancellableBookings = rideFound.RideBookings
				.Where(booking =>
					!booking.IsDeleted &&
					booking.Status != BookingStatus.CancelledByRider &&
					booking.Status != BookingStatus.CancelledByDriver &&
					booking.Status != BookingStatus.Expired)
				.ToList();

			var cancellationsToNotify = new List<(string UserId, int BookingId)>();
			var walletReleasesToNotify = new List<(string UserId, int WalletId, decimal Amount)>();
			await using var tx = await _unitOfWork.BeginTransactionAsync();
			try
			{
				foreach (var booking in cancellableBookings)
				{
					var cancelResult = await _bookingService.CancelBooking(booking.Id, BookingCancellationType.Driver);
					if (cancelResult.IsFailure)
					{
						await tx.RollbackAsync();
						return Result.Failure(cancelResult.Message);
					}

					var releaseResult = await ReleaseLockedAmountForBooking(booking.Id, booking.UserId);
					if (releaseResult.IsFailure)
					{
						_logger.LogError("CancelRide: booking {BookingId} failed to release amount for user {UserId}. Error: {Error}", booking.Id, booking.UserId, releaseResult.Message);
						await tx.RollbackAsync();
						return Result.Failure($"Booking cancelled but failed to release locked amount. {releaseResult.Message}");
					}

					cancellationsToNotify.Add((booking.UserId, booking.Id));

					if (releaseResult.Data > 0)
					{
						var riderWallet = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == booking.UserId);
						if (riderWallet != null)
						{
							walletReleasesToNotify.Add((booking.UserId, riderWallet.Id, releaseResult.Data));
						}
					}
				}

				rideFound.Status = RideStatus.Cancelled;
				rideFound.UpdatedAt = DateTime.UtcNow;
				_unitOfWork.Rides.Update(rideFound);

				await _unitOfWork.SaveChangesAsync();
				await tx.CommitAsync();

				foreach (var (userId, walletId, amount) in walletReleasesToNotify)
				{
					await SendWalletAmountReleasedNotification(userId, walletId, amount);
					await _cache.RemoveWalletCachesAsync(userId);
				}

				if (cancellationsToNotify.Any())
				{
					await SendRideCancellationNotifications(cancellationsToNotify);
				}

				return Result.Success("Ride cancelled successfully");
			}
			catch (Exception ex)
			{
				await tx.RollbackAsync();
				_logger.LogError(ex, "Critical error during ride cancellation");
				return Result.Failure("An internal error occurred.");
			}
		}
		private async Task SendRideCancellationNotifications(IEnumerable<(string UserId, int BookingId)> cancellations)
		{
			foreach (var (userId, bookingId) in cancellations)
			{
				var metadata = new NotificationMetadata { Preview = "Your ride request has been Canceled" };

				var input = new NotificationInput(
					userId,
					NotificationCategory.Ride,
					NotificationEventCode.bookingCanceled,
					bookingId.ToString(),
					metadata
				);

				await _notificationOrchestrator.NotifyAsync(input);
			}
		}
		private async Task SendRequestNotification(string driverId, int bookingId)
		{
			var metadata = new NotificationMetadata { Preview = "You have a new Ride Request" };
			var input = new NotificationInput(driverId, NotificationCategory.Ride, NotificationEventCode.bookingRequest, bookingId.ToString(), metadata);
			await _notificationOrchestrator.NotifyAsync(input);
		}
		private async Task<Result<decimal>> ReleaseLockedAmountForBooking(int bookingId, string userId)
		{
			var lockTransactions = await _unitOfWork.WalletTransactions.FindAllAsNoTrackingAsync(
				x => x.RideBookingId == bookingId && x.Type == TransactionType.locked);

			var totalLockedAmount = lockTransactions.Sum(x => x.Amount);
			if (totalLockedAmount <= 0)
			{
				_logger.LogInformation("ReleaseLockedAmountForBooking: no locked amount found for booking {BookingId}", bookingId);
				return Result<decimal>.Success(0);
			}

			var releaseResult = await _walletService.ReleaseLockedAmountAsync(userId, totalLockedAmount);
			if (releaseResult.IsFailure)
			{
				return Result<decimal>.Failure(releaseResult.Message);
			}

			return Result<decimal>.Success(totalLockedAmount);
		}
		private async Task SendAcceptNotification(string userId, int bookingId)
		{
			var metadata = new NotificationMetadata { Preview = "Your ride request has been accepted" };
			var input = new NotificationInput(userId, NotificationCategory.Ride, NotificationEventCode.bookingAccepted, bookingId.ToString(), metadata);
			await _notificationOrchestrator.NotifyAsync(input);
		}
		private async Task SendDeclineNotification(string userId, int bookingId)
		{
			var metadata = new NotificationMetadata { Preview = "Your ride request has been declined" };
			var input = new NotificationInput(userId, NotificationCategory.Ride, NotificationEventCode.bookingDeclined, bookingId.ToString(), metadata);
			await _notificationOrchestrator.NotifyAsync(input);
		}
		private async Task SendWalletAmountReservedNotification(string userId, int walletId, decimal amount)
		{
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
				walletId.ToString(),
				metadata);

			await _notificationOrchestrator.NotifyAsync(notification);
		}
		private async Task SendWalletAmountReleasedNotification(string userId, int walletId, decimal amount)
		{
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
				walletId.ToString(),
				metadata);

			await _notificationOrchestrator.NotifyAsync(notification);
		}
	}
}