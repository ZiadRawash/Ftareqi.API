using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.DTOs.Rides;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Mappers;
using Ftareqi.Domain.Constants;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Ftareqi.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class RideService : IRideService
	{
		private readonly INotificationOrchestrator _notificationService;

		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<RideService> _logger;

		public RideService(IUnitOfWork unitOfWork, ILogger<RideService> logger, INotificationOrchestrator notificationService)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_notificationService = notificationService;
		}

		public async Task<Result> CreateRide(CreateRideRequestDto model, string userId)
		{
			if (model == null)
				return Result.Failure("Ride data is required");

			var profileFound = await _unitOfWork.DriverProfiles.FirstOrDefaultAsNoTrackingAsync(
				x => x.UserId == userId && x.Status == Domain.Enums.DriverStatus.Active);

			if (profileFound == null)
			{
				_logger.LogInformation("Could not create ride for user {UserId}: active driver profile not found", userId);
				return Result.Failure("Unable to create ride");
			}
			try
			{
				var ride = model.ToEntity(profileFound.Id);

				if (ride.RidePreferences == null)
					return Result.Failure("Ride preferences are required");
				await _unitOfWork.Rides.AddAsync(ride);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Ride {RideId} created successfully for user {UserId}", ride.Id, userId);
				return Result.Success("Ride created successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while creating ride for user {UserId}", userId);
				return Result.Failure("Unexpected error happened while creating ride");
			}
		}

		public async Task<Result<PaginatedResponse<DriverPastRidesResponse>>> GetDriverPastRides(GenericQueryReq request, string userId)
		{
			try
			{

				var profileFound = await _unitOfWork.DriverProfiles.FirstOrDefaultAsNoTrackingAsync(
					x => x.UserId == userId && !x.IsDeleted);

				if (profileFound == null)
				{
					_logger.LogWarning("Access denied for GetDriverPastRides: Driver profile not found or deleted for User {UserId}.", userId);
					return Result<PaginatedResponse<DriverPastRidesResponse>>.Failure("No driver profile is found");
				}

				var now = DateTime.UtcNow;
				var (rides, count) = await _unitOfWork.Rides.GetPagedAsync(
					request.Page,
					request.PageSize,
					x => x.CreatedAt,
					x => x.DriverProfileId == profileFound.Id &&
						 (x.DepartureTime <= now || x.Status == RideStatus.Completed || x.Status == RideStatus.Cancelled),
					true);

				_logger.LogInformation("Successfully retrieved {Count} total past rides ({CurrentBatchCount} in current page) for Driver {ProfileId}.",
					count, rides.Count(), profileFound.Id);

				var items = rides.Select(ride =>
				{
					var takenSeats = Math.Max(0, ride.TotalSeats - ride.AvailableSeats);
					return new DriverPastRidesResponse
					{
						RideId = ride.Id,
						StartLatitude = ride.StartLocation.Y,
						StartLongitude = ride.StartLocation.X,
						StartAddress = ride.StartAddress,
						EndLatitude = ride.EndLocation.Y,
						EndLongitude = ride.EndLocation.X,
						EndAddress = ride.EndAddress,
						DepartureTime = ride.DepartureTime,
						Status = ride.Status,
						TakenSeats = takenSeats,
						TotalEarnings = takenSeats * ride.PricePerSeat,
						AverageRating = 0f
					};
				}).ToList();

				var totalPages = (int)Math.Ceiling((double)count / request.PageSize);

				var response = new PaginatedResponse<DriverPastRidesResponse>
				{
					Page = request.Page,
					PageSize = request.PageSize,
					TotalCount = count,
					TotalPages = totalPages,
					Items = items
				};

				return Result<PaginatedResponse<DriverPastRidesResponse>>.Success(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while getting past rides for user {UserId}. Request: {@Request}",
					userId, request);
				return Result<PaginatedResponse<DriverPastRidesResponse>>.Failure("Unexpected error happened while getting past rides");
			}
		}

		public async Task<Result<PaginatedResponse<DriverUpcomingRidesResponse>>> GetDriverUpcomingRides(GenericQueryReq request, string userId)
		{
			try
			{
				_logger.LogInformation("Fetching upcoming rides for User: {UserId}. Page: {Page}, PageSize: {PageSize}",
					userId, request.Page, request.PageSize);

				var profileFound = await _unitOfWork.DriverProfiles.FirstOrDefaultAsNoTrackingAsync(
					x => x.UserId == userId && !x.IsDeleted);

				if (profileFound == null)
				{
					_logger.LogWarning("Driver profile not found or deleted for user {UserId}", userId);
					return Result<PaginatedResponse<DriverUpcomingRidesResponse>>.Failure("No driver profile is found");
				}

				var now = DateTime.UtcNow;

				var (rides, count) = await _unitOfWork.Rides.GetPagedAsync(
					request.Page,
					request.PageSize,
					x => x.CreatedAt,
					x => x.DriverProfileId == profileFound.Id &&
						 x.DepartureTime > now &&
						 x.Status == RideStatus.Scheduled,
					true,
					x => x.RidePreferences);
				_logger.LogInformation("Successfully retrieved {Count} total upcoming rides ({CurrentBatchCount} in current page) for driver {ProfileId}.",
					count, rides.Count(), profileFound.Id);

				var items = rides.Select(ride => new DriverUpcomingRidesResponse
				{
					RideId = ride.Id,
					StartLatitude = ride.StartLocation.Y,
					StartLongitude = ride.StartLocation.X,
					StartAddress = ride.StartAddress,
					EndLatitude = ride.EndLocation.Y,
					EndLongitude = ride.EndLocation.X,
					EndAddress = ride.EndAddress,
					TotalSeats = ride.TotalSeats,
					AvailableSeats = ride.AvailableSeats,
					PricePerSeat = ride.PricePerSeat,
					Status = ride.Status,
					WaitingTimeMinutes = (int)ride.WaitingTime.TotalMinutes,
					DepartureTime = ride.DepartureTime,
					MusicAllowed = ride.RidePreferences?.MusicAllowed ?? false,
					NoSmoking = ride.RidePreferences?.NoSmoking ?? false,
					OpenToConversation = ride.RidePreferences?.OpenToConversation ?? false,
					PetsWelcomed = ride.RidePreferences?.PetsWelcomed ?? false

				}).ToList();

				var totalPages = (int)Math.Ceiling((double)count / request.PageSize);

				var response = new PaginatedResponse<DriverUpcomingRidesResponse>
				{
					Page = request.Page,
					PageSize = request.PageSize,
					TotalCount = count,
					TotalPages = totalPages,
					Items = items
				};

				return Result<PaginatedResponse<DriverUpcomingRidesResponse>>.Success(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while getting upcoming rides for user {UserId}. Request: {@Request}",
					userId, request);
				return Result<PaginatedResponse<DriverUpcomingRidesResponse>>.Failure("Unexpected error happened while getting upcoming rides");
			}
		}
		public async Task<Result<PaginatedResponse<RideSearchResponseDto>>> SearchForRides(RideSearchRequestDto requestDto, string userId)
		{
			if (requestDto == null)
				return Result<PaginatedResponse<RideSearchResponseDto>>.Failure("Ride search data is required");

			try
			{


				var (items, totalCount) = await _unitOfWork.Rides.SearchForRidesAsync(
					requestDto,
					userId);

				var response = new PaginatedResponse<RideSearchResponseDto>
				{
					Page = requestDto.Page,
					PageSize = requestDto.PageSize,
					TotalCount = totalCount,
					TotalPages = (int)Math.Ceiling((double)totalCount / requestDto.PageSize),
					Items = items.ToList()
				};

				_logger.LogInformation("Ride search completed for user {UserId} with filter {Filter}. Found {Count} rides.",
					userId, requestDto.Filters, totalCount);

				return Result<PaginatedResponse<RideSearchResponseDto>>.Success(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while searching rides for user {UserId}. Request: {@Request}", userId, requestDto);
				return Result<PaginatedResponse<RideSearchResponseDto>>.Failure("Unexpected error happened while searching for rides");
			}
		}

		public async Task<Result> ArriveAtStartLocation(CheckInRequestDto model, int rideId)
		{
			_logger.LogInformation("Check-in attempt for ride {RideId} at location Latitude: {Latitude}, Longitude: {Longitude}", 
				rideId, model.Latitude, model.Longitude);

			var rideFound = await _unitOfWork.Rides.FirstOrDefaultAsync(x => x.Id == rideId, x => x.RideBookings);

			if (rideFound == null)
			{
				_logger.LogWarning("Check-in failed: Ride {RideId} not found", rideId);
				return Result.Failure("Invalid ride id");
			}

			if (rideFound.Status != RideStatus.Scheduled)
			{
				_logger.LogWarning("Check-in failed for ride {RideId}: Invalid ride status. Current status: {Status}", 
					rideId, rideFound.Status);
				return Result.Failure("Invalid operation");
			}

			if (rideFound.Status == RideStatus.CheckedIn)
			{
				_logger.LogWarning("Check-in failed for ride {RideId}: Driver already checked in", rideId);
				return Result.Failure("Driver already checked in");
			}

			var isValidLocation = LocationHelper.IsWithinRadius(
				rideFound.StartLocation,
				model.Latitude,
				model.Longitude,
				RidePolicies.ArrivalRadiusMeters);

			if (!isValidLocation)
			{
				_logger.LogWarning("Check-in failed for ride {RideId}: Location validation failed. Driver position Latitude: {Latitude}, Longitude: {Longitude} is {DistanceMeters}m from pickup point",
					rideId, model.Latitude, model.Longitude, RidePolicies.ArrivalRadiusMeters);
				return Result.Failure("You are too far from the pickup point");
			}

			int driverWaitingTimeMinutes = (int)rideFound.WaitingTime.TotalMinutes;

			var arrivalStatus = GetStatus(
				DateTimeOffset.UtcNow,
				rideFound.DepartureTime,
				driverWaitingTimeMinutes,
				RidePolicies.ArrivalGracePeriodMinutes);

			if (arrivalStatus == ArrivalStatus.Early)
			{
				_logger.LogInformation("Check-in rejected for ride {RideId}: Driver arrived too early", rideId);
				return Result.Failure("You arrived too early. Wait couple of minutes");
			}

			if (arrivalStatus == ArrivalStatus.Late)
			{
				_logger.LogInformation("Check-in completed for ride {RideId} with late arrival status. Strike will be issued", rideId);
				// TODO: Call Strike Service here
			}

			rideFound.Status = RideStatus.CheckedIn;
			_unitOfWork.Rides.Update(rideFound);
			await _unitOfWork.SaveChangesAsync();

			_logger.LogInformation("Ride {RideId} status updated to CheckedIn. Arrival Status: {ArrivalStatus}", 
				rideId, arrivalStatus);

			string successMessage = arrivalStatus == ArrivalStatus.Late
				? "Check-in successful, but you've received a Strike for being late. Please stick to the schedule next time."
				: "Check-in successful. You are right on time!";
			var ids = rideFound.RideBookings.Select(x => x.UserId).ToList();

			_logger.LogInformation("Sending check-in notification to {PassengerCount} passengers for ride {RideId}", 
				ids.Count, rideId);

			var notificationSent = await DriverCheckedInNotification(rideId, ids);
			if (notificationSent.IsFailure)
			{
				_logger.LogError("Failed to send check-in notification for ride {RideId}", rideId);
			}

			_logger.LogInformation("Check-in successfully completed for ride {RideId}. Message: {Message}", 
				rideId, successMessage);

			return Result.Success(successMessage);
		}

		private ArrivalStatus GetStatus(DateTimeOffset currentTime, DateTimeOffset departureTime, int driverWaitingTime, int gracePeriod)
		{
			//the right time
			var idealArrival = departureTime.AddMinutes(-driverWaitingTime);

			// rn - the right time
			var diff = (currentTime - idealArrival).TotalMinutes;

			// too early
			if (diff < -gracePeriod)
			{
				return ArrivalStatus.Early;
			}

			// on time
			if (Math.Abs(diff) <= gracePeriod)
			{
				return ArrivalStatus.OnTime;
			}

			//late
			return ArrivalStatus.Late;
		}
		private async Task<Result> DriverCheckedInNotification(int rideId, List<string> ids)
		{
			try
			{
				var metadata = new NotificationMetadata
				{
					Preview = "driver has arrived at the starting point"
				};

				var notificationTasks = ids.Select(userId =>
					_notificationService.NotifyAsync(new NotificationInput(
						userId,
						NotificationCategory.Ride,
						NotificationEventCode.DriveCheckedIn,
						rideId.ToString(),
						metadata))
				).ToList();

				await Task.WhenAll(notificationTasks);
				return Result.Success("Notifications sent");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending check-in notification for ride {RideId}", rideId);
				return Result.Failure("Failed to send notifications");
			}
		}

	}
}
