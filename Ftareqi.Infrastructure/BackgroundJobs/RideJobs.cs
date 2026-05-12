using Ftareqi.Application.DTOs.Notification;
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
	public class RideJobs : IRideJobs
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWalletService _walletService;
		private readonly INotificationOrchestrator _notificationOrchestrator;
		private readonly ILogger<RideJobs> _logger;

		public RideJobs(
			IUnitOfWork unitOfWork,
			IWalletService walletService,
			INotificationOrchestrator notificationOrchestrator,
			ILogger<RideJobs> logger)
		{
			_unitOfWork = unitOfWork;
			_walletService = walletService;
			_notificationOrchestrator = notificationOrchestrator;
			_logger = logger;
		}

		public async Task HandleNotStartedRidesAsync(int rideId)
		{
			var timeRn = DateTime.UtcNow;
			var ride = await _unitOfWork.Rides.FirstOrDefaultAsync(x => x.Id == rideId, x => x.RideBookings, x => x.DriverProfile);
			if (ride == null)
			{
				_logger.LogWarning("HandleNotStartedRidesAsync: Ride {RideId} not found", rideId);
				return;
			}

			ride.Status = RideStatus.Cancelled;
			_unitOfWork.Rides.Update(ride);

			//penalize driver
			int failureCount = 0;
			foreach (var booking in ride.RideBookings)
			{
				var moneyReturned = await _walletService.ReleaseLockedAmountAsync(booking.UserId, ride.PricePerSeat * booking.NumOfSeats);
				if (moneyReturned.IsFailure)
				{
					_logger.LogError("HandleNotStartedRidesAsync: Failed to release locked amount for booking {BookingId}, user {UserId}", booking.Id, booking.UserId);
					failureCount++;
				}
				booking.Status = BookingStatus.CancelledByDriver;
				booking.CancelledAt = DateTime.UtcNow;
				booking.UpdatedAt = DateTime.UtcNow;
			}

			_unitOfWork.RideBookings.UpdateRange(ride.RideBookings);
			await _unitOfWork.SaveChangesAsync();

			//send notification for driver
			var driverMetadata = new NotificationMetadata { Preview = "Ride cancelled due to being late. You will be penalized." };
			var driverNotification = new NotificationInput(ride.DriverProfile.UserId, NotificationCategory.Ride, NotificationEventCode.RideCancelled, rideId.ToString(), driverMetadata);
			try
			{
				await _notificationOrchestrator.NotifyAsync(driverNotification);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "HandleNotStartedRidesAsync: Failed to send driver notification for ride {RideId}", rideId);
			}

			//send notification for riders
			foreach (var booking in ride.RideBookings)
			{
				var riderMetadata = new NotificationMetadata { Preview = "Trip automatically cancelled: The ride didn't start on time." };
				var riderNotification = new NotificationInput(booking.UserId, NotificationCategory.Ride, NotificationEventCode.RideCancelled, rideId.ToString(), riderMetadata);
				try
				{
					await _notificationOrchestrator.NotifyAsync(riderNotification);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "HandleNotStartedRidesAsync: Failed to send rider notification for booking {BookingId}", booking.Id);
				}
			}

			if (failureCount > 0)
			{
				_logger.LogWarning("HandleNotStartedRidesAsync: Completed for ride {RideId} with {FailureCount} wallet release failures", rideId, failureCount);
			}
			else
			{
				_logger.LogInformation("HandleNotStartedRidesAsync: Successfully completed for ride {RideId}", rideId);
			}
		}
	}
}
