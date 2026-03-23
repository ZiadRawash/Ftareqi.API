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
		private readonly IBookingService _bookingService;
		private readonly INotificationOrchestrator _notificationOrchestrator;
		private const int ExpiredBookingsBatchSize = 200;
		public RideOrchestrator(ILogger<RideOrchestrator> logger, IUnitOfWork unitOfWork,
			IRideService rideService, IWalletService walletService,
			 IBookingService bookingService,
			 INotificationOrchestrator notificationOrchestrator
			 )
		{

			_logger = logger;
			_unitOfWork = unitOfWork;
			_rideService = rideService;
			_walletService = walletService;
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
			var moneyLocked = await _walletService.LockAmountAsync(userId, totalMoney);
			if (moneyLocked.IsFailure )
			{
				return Result.Failure(moneyLocked.Message);
			}
			var reservedId =await _bookingService.CreateBooking(model, userId);
			if (reservedId.IsFailure)
			{
				var releaseResult = await _walletService.ReleaseLockedAmountAsync(userId, totalMoney);
				if (releaseResult.IsFailure)
				{
					_logger.LogError("Failed to release locked amount for user {UserId} after booking create failure. BookingError: {BookingError}, ReleaseError: {ReleaseError}", userId, reservedId.Message, releaseResult.Message);
					return Result.Failure($"{reservedId.Message}. Also failed to release locked amount, please contact support.");
				}
				return Result.Failure(reservedId.Message);
			}
			var userWallet = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == userId);
			if (userWallet == null)
			{
				_logger.LogError("Failed to find wallet for user {UserId} after successful booking creation {BookingId}", userId, reservedId.Data);
				return Result.Failure("Booking created but failed to link wallet transaction to booking");
			}
			var lockTransactions = await _unitOfWork.WalletTransactions.FindAllAsTrackingAsync(
				x => x.UserWalletId == userWallet.Id
					 && x.Type == TransactionType.locked
					 && x.Amount == totalMoney
					 && x.RideBookingId == null);

			var lockTransaction = lockTransactions
				.OrderByDescending(x => x.CreatedAt)
				.ThenByDescending(x => x.Id)
				.FirstOrDefault();
			if (lockTransaction == null)
			{
				_logger.LogError("Failed to find lock transaction for user {UserId} and booking {BookingId}", userId, reservedId.Data);
				return Result.Failure("Booking created but failed to link wallet transaction to booking");
			}
			lockTransaction.RideBookingId = reservedId.Data;
			lockTransaction.UpdatedAt = DateTime.UtcNow;
			_unitOfWork.WalletTransactions.Update(lockTransaction);
			await _unitOfWork.SaveChangesAsync();
			await SendRequestNotification(driverProfile.UserId, reservedId.Data);
			return Result.Success("Booking Created successfully");
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

			var cancelResult = await _bookingService.CancelBooking(bookingId, BookingCancellationType.Driver);
			if (cancelResult.IsFailure)
			{
				return Result.Failure(cancelResult.Message);
			}

			var releaseResult = await ReleaseLockedAmountForBooking(bookingId, booking.UserId);
			if (releaseResult.IsFailure)
			{
				_logger.LogError("DeclineRideBookingRequest: booking {BookingId} declined but failed to release locked amount for user {UserId}. Error: {Error}", bookingId, booking.UserId, releaseResult.Message);
				return Result.Failure($"Booking declined but failed to release locked amount. {releaseResult.Message}");
			}

			await SendDeclineNotification(booking.UserId, bookingId);

			return Result.Success("Booking declined successfully");
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

			var cancelResult = await _bookingService.CancelBooking(bookingId, BookingCancellationType.Rider);
			if (cancelResult.IsFailure)
			{
				return Result.Failure(cancelResult.Message);
			}

			var releaseResult = await ReleaseLockedAmountForBooking(bookingId, riderId);
			if (releaseResult.IsFailure)
			{
				_logger.LogError("CancelRideBookingByRider: booking {BookingId} cancelled by rider but failed to release locked amount for user {UserId}. Error: {Error}", bookingId, riderId, releaseResult.Message);
				return Result.Failure($"Booking cancelled but failed to release locked amount. {releaseResult.Message}");
			}

			return Result.Success("Booking cancelled successfully");
		}

		public async Task<Result> HandleExpiredBookings()
		{
			var now = DateTime.UtcNow;
			var (expiredBookingsPage, totalExpiredCount) = await _unitOfWork.RideBookings.GetPagedAsync(
				pageNumber: 1,
				pageSize: ExpiredBookingsBatchSize,
				orderBy: x => x.ExpiresAt,
				predicate: x => x.Status == BookingStatus.Pending && x.ExpiresAt <= now);

			var expiredBookings = expiredBookingsPage.ToList();
			if (!expiredBookings.Any())
			{
				_logger.LogInformation("HandleExpiredBookings: no pending bookings to expire at {Now}", now);
				return Result.Success("No expired bookings to process");
			}

			var bookingIds = expiredBookings.Select(x => x.Id).ToList();
			var lockTransactions = !bookingIds.Any()? Enumerable.Empty<WalletTransaction>()
				: await _unitOfWork.WalletTransactions.FindAllAsNoTrackingAsync(
					x => x.RideBookingId.HasValue
						&& bookingIds.Contains(x.RideBookingId.Value)
						&& x.Type == TransactionType.locked);

			var lockTotalsByBooking = lockTransactions
				.GroupBy(x => x.RideBookingId!.Value)
				.ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

			int successCount = 0;
			int failureCount = 0;

			foreach (var booking in expiredBookings)
			{
				var expireResult = await _bookingService.ExpireBooking(booking.Id);
				if (expireResult.IsFailure)
				{
					failureCount++;
					_logger.LogError("HandleExpiredBookings: failed to expire booking {BookingId}. Error: {Error}", booking.Id, expireResult.Message);
					continue;
				}

				if (!lockTotalsByBooking.TryGetValue(booking.Id, out var totalLockedAmount))
				{
					_logger.LogWarning("HandleExpiredBookings: no lock transactions found for expired booking {BookingId}", booking.Id);
					successCount++;
					continue;
				}

				if (totalLockedAmount <= 0)
				{
					_logger.LogWarning("HandleExpiredBookings: total locked amount is zero for booking {BookingId}", booking.Id);
					successCount++;
					continue;
				}

				var releaseResult = await _walletService.ReleaseLockedAmountAsync(booking.UserId, totalLockedAmount);
				if (releaseResult.IsFailure)
				{
					failureCount++;
					_logger.LogError("HandleExpiredBookings: failed to release locked amount for booking {BookingId} and user {UserId}. Error: {Error}", booking.Id, booking.UserId, releaseResult.Message);
					continue;
				}

				_logger.LogInformation("HandleExpiredBookings: successfully expired booking {BookingId} and released {Amount} for user {UserId}", booking.Id, totalLockedAmount, booking.UserId);
				successCount++;
			}

			var remaining = totalExpiredCount - expiredBookings.Count;
			if (remaining > 0)
			{
				_logger.LogInformation("HandleExpiredBookings: {Remaining} additional expired bookings will be handled in subsequent runs", remaining);
			}

			if (failureCount == 0)
			{
				return Result.Success($"Successfully processed {successCount} expired bookings");
			}

			return Result.Failure($"Processed {successCount} expired bookings with {failureCount} failures");
		}
		private async Task SendRequestNotification(string driverId, int bookingId)
		{
			var metadata = new NotificationMetadata { Preview = "You have a new Ride Request" };
			var input = new NotificationInput(driverId, NotificationCategory.Ride, NotificationEventCode.bookingRequest, bookingId.ToString(), metadata);
			await _notificationOrchestrator.NotifyAsync(input);
		}

		private async Task<Result> ReleaseLockedAmountForBooking(int bookingId, string userId)
		{
			var lockTransactions = await _unitOfWork.WalletTransactions.FindAllAsNoTrackingAsync(
				x => x.RideBookingId == bookingId && x.Type == TransactionType.locked);

			var totalLockedAmount = lockTransactions.Sum(x => x.Amount);
			if (totalLockedAmount <= 0)
			{
				_logger.LogInformation("ReleaseLockedAmountForBooking: no locked amount found for booking {BookingId}", bookingId);
				return Result.Success();
			}

			var releaseResult = await _walletService.ReleaseLockedAmountAsync(userId, totalLockedAmount);
			if (releaseResult.IsFailure)
			{
				return Result.Failure(releaseResult.Message);
			}

			return Result.Success();
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
	}
}