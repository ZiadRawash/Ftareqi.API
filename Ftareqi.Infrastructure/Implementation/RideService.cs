using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Rides;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Mappers;
using Ftareqi.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class RideService : IRideService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<RideService> _logger;

		public RideService(IUnitOfWork unitOfWork, ILogger<RideService> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
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

				var items = rides.Select(ride => {
					var takenSeats = Math.Max(0, ride.TotalSeats - ride.AvailableSeats);
					return new DriverPastRidesResponse
					{
						StartLatitude = ride.StartLocation.Y,
						StartLongitude = ride.StartLocation.X,
						EndLatitude = ride.EndLocation.Y,
						EndLongitude = ride.EndLocation.X,
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
					true);
				_logger.LogInformation("Successfully retrieved {Count} total upcoming rides ({CurrentBatchCount} in current page) for driver {ProfileId}.",
					count, rides.Count(), profileFound.Id);

				var items = rides.Select(ride => new DriverUpcomingRidesResponse
				{
					StartLatitude = ride.StartLocation.Y,
					StartLongitude = ride.StartLocation.X,
					EndLatitude = ride.EndLocation.Y,
					EndLongitude = ride.EndLocation.X,
					TotalSeats = ride.TotalSeats,
					AvailableSeats = ride.AvailableSeats,
					PricePerSeat = ride.PricePerSeat,
					Status = ride.Status,
					WaitingTimeMinutes = (int)ride.WaitingTime.TotalMinutes,
					DepartureTime = ride.DepartureTime
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
				var (items, totalCount) = await _unitOfWork.Rides.SearchForRidesAsync(requestDto,userId);
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
				throw;
			}
		}
	}
}
