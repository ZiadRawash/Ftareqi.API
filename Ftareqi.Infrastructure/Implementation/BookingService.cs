using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Bookings;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Mappers;
using Ftareqi.Application.QueryEnums;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Ftareqi.Infrastructure.Implementation
{
	public class BookingService : IBookingService
	{
		private static readonly TimeSpan PendingBookingExpirationWindow = TimeSpan.FromHours(2);

		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<BookingService> _logger;

		public BookingService(IUnitOfWork unitOfWork, ILogger<BookingService> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<Result<int>> CreateBooking(CreateBookingRequestDto request, string userId)
		{
			if (request == null)
				return Result<int>.Failure("Booking data is required");

			if (string.IsNullOrWhiteSpace(userId))
				return Result<int>.Failure("User id is required");

			if (request.RideId <= 0)
				return Result<int>.Failure("Valid ride id is required");

			if (request.NumberOfSeats <= 0)
				return Result<int>.Failure("Number of seats must be greater than zero");

			try
			{
				var now = DateTime.UtcNow;
				var ride = await _unitOfWork.Rides.FirstOrDefaultAsync(
					x => x.Id == request.RideId,
					x => x.DriverProfile,
					x => x.RideBookings);

				if (ride == null || ride.DriverProfile == null)
					return Result<int>.Failure("Ride not found");

				if (ride.DriverProfile.IsDeleted)
					return Result<int>.Failure("Ride is not available for booking");

				if (ride.DriverProfile.UserId == userId)
					return Result<int>.Failure("You cannot book your own trip");

				if (ride.Status != RideStatus.Scheduled)
					return Result<int>.Failure("Ride is not available for booking");

				if (ride.DepartureTime <= now)
					return Result<int>.Failure("Cannot create booking request after departure time");

				if (request.NumberOfSeats > ride.AvailableSeats)
					return Result<int>.Failure("Requested seats exceed available seats");

				var hasExistingActiveBooking = await _unitOfWork.RideBookings.ExistsAsync(
					x => x.RideId == request.RideId &&
						 x.UserId == userId &&
						 !x.IsDeleted &&
						 (x.Status == BookingStatus.Pending ||
						  x.Status == BookingStatus.Accepted ));
				if (hasExistingActiveBooking)
					return Result<int>.Failure("You already have an active booking request for this ride");

				var booking = new RideBooking
				{
					RideId = request.RideId,
					UserId = userId,
					NumOfSeats = request.NumberOfSeats,
					Status = BookingStatus.Pending,
					BookedAt = now,
					CancelledAt = now,
					CreatedAt = now,
					UpdatedAt = now,
					IsDeleted = false
				};
				await _unitOfWork.RideBookings.AddAsync(booking);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Booking {BookingId} created by user {UserId} for ride {RideId}", booking.Id, userId, request.RideId);
				return Result<int>.Success(booking.Id, "Booking request created successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while creating booking for user {UserId}", userId);
				return Result<int>.Failure("Unexpected error happened while creating booking");
			}
		}

		public async Task<Result<UserTripRequestResponseDto>> GetBookingById(int bookingId)
		{
			if (bookingId <= 0)
				return Result<UserTripRequestResponseDto>.Failure("Valid booking id is required");

			try
			{
				var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
					x => x.Id == bookingId && !x.IsDeleted,
					x => x.Ride,
					x => x.Ride!.DriverProfile!,
					x => x.Ride!.DriverProfile!.User!);

				if (booking == null)
					return Result<UserTripRequestResponseDto>.Failure("Booking not found");

				var dto = booking.ToUserTripRequestDto();
				return Result<UserTripRequestResponseDto>.Success(dto);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while getting booking {BookingId}", bookingId);
				return Result<UserTripRequestResponseDto>.Failure("Unexpected error happened while getting booking details");
			}
		}

		public async Task<Result<PaginatedResponse<DriverTripRequestResponseDto>>> GetDriverTripRequests(GetTripsRequestsDto request, string driverUserId)
		{
			if (request == null)
				return Result<PaginatedResponse<DriverTripRequestResponseDto>>.Failure("Request data is required");

			try
			{
				var profile = await _unitOfWork.DriverProfiles.FirstOrDefaultAsNoTrackingAsync(
					x => x.UserId == driverUserId && !x.IsDeleted && x.Status == DriverStatus.Active);

				if (profile == null)
					return Result<PaginatedResponse<DriverTripRequestResponseDto>>.Failure("No active driver profile found");

				var statusFilter = MapQueryStatus(request.FilterBy);
				var now = DateTime.UtcNow;
				var (bookings, totalCount) = await _unitOfWork.RideBookings.GetPagedAsync(
					request.Page,
					request.PageSize,
					x => x.BookedAt,
					x => !x.IsDeleted &&
						 x.Ride.DriverProfileId == profile.Id &&
						 x.Ride.DepartureTime >= now &&
						 (x.Status == BookingStatus.Pending || x.Status == BookingStatus.Accepted) &&
						 (!statusFilter.HasValue || x.Status == statusFilter.Value),
					request.SortDescending,
					x => x.User,
					x => x.Ride);

				var items = bookings.Select(x => x.ToDriverTripRequestDto()).ToList();

				var response = BuildPaginatedResponse(items, request.Page, request.PageSize, totalCount);
				return Result<PaginatedResponse<DriverTripRequestResponseDto>>.Success(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while getting driver trip requests for user {UserId}", driverUserId);
				return Result<PaginatedResponse<DriverTripRequestResponseDto>>.Failure("Unexpected error happened while getting driver trip requests");
			}
		}

		public async Task<Result<PaginatedResponse<UserTripRequestResponseDto>>> GetUserUpcomingTripRequests(GetUpcomingTripsRequestsDto request, string userId)
		{
			if (request == null)
				return Result<PaginatedResponse<UserTripRequestResponseDto>>.Failure("Request data is required");

			try
			{
				var statusFilter = MapUpcomingQueryStatus(request.FilterBy);
				var now = DateTime.UtcNow;
				var (bookings, totalCount) = await _unitOfWork.RideBookings.GetPagedAsync(
					request.Page,
					request.PageSize,
					x => x.BookedAt,
					x => !x.IsDeleted &&
						 x.UserId == userId &&
						 x.Ride.DepartureTime >= now &&
						 (x.Status == BookingStatus.Pending || x.Status == BookingStatus.Accepted) &&
						 (!statusFilter.HasValue || x.Status == statusFilter.Value),
					request.SortDescending,
					x => x.Ride,
					x => x.Ride!.DriverProfile!,
					x => x.Ride!.DriverProfile!.User!);

				var items = bookings.Select(x => x.ToUserTripRequestDto()).ToList();
				var response = BuildPaginatedResponse(items, request.Page, request.PageSize, totalCount);

				return Result<PaginatedResponse<UserTripRequestResponseDto>>.Success(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while getting upcoming trip requests for user {UserId}", userId);
				return Result<PaginatedResponse<UserTripRequestResponseDto>>.Failure("Unexpected error happened while getting upcoming trip requests");
			}
		}

		public async Task<Result<PaginatedResponse<UserTripRequestResponseDto>>> GetUserPastTripRequests(GenericQueryReq request, string userId)
		{
			if (request == null)
				return Result<PaginatedResponse<UserTripRequestResponseDto>>.Failure("Request data is required");
			try
			{
				var now = DateTime.UtcNow;
				var (bookings, totalCount) = await _unitOfWork.RideBookings.GetPagedAsync(
					request.Page,
					request.PageSize,
					x => x.BookedAt,
					x => !x.IsDeleted &&
						 x.UserId == userId &&
						 (x.Ride.DepartureTime < now &&
						  x.Status == BookingStatus.Accepted || x.Status== BookingStatus.CancelledByDriver),
					request.SortDescending,
					x => x.Ride,
					x => x.Ride!.DriverProfile!,
					x => x.Ride!.DriverProfile!.User!);
				var items = bookings.Select(x => x.ToUserTripRequestDto()).ToList();
				var response = BuildPaginatedResponse(items, request.Page, request.PageSize, totalCount);

				return Result<PaginatedResponse<UserTripRequestResponseDto>>.Success(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while getting past trip requests for user {UserId}", userId);
				return Result<PaginatedResponse<UserTripRequestResponseDto>>.Failure("Unexpected error happened while getting past trip requests");
			}
		}

		public async Task<Result> ExpireBooking(int bookingId)
		{
			if (bookingId <= 0)
				return Result.Failure("Valid booking id is required");

			try
			{
				var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
					x => x.Id == bookingId &&
						 !x.IsDeleted,
					x => x.Ride);

				if (booking == null)
					return Result.Failure("Booking not found");

				if (booking.Status == BookingStatus.CancelledByRider || booking.Status == BookingStatus.CancelledByDriver || booking.Status == BookingStatus.Expired)
					return Result.Failure("Booking is already cancelled or expired");

				if (booking.Status != BookingStatus.Pending)
					return Result.Failure("Only pending bookings can be expired");

				if (DateTime.UtcNow - booking.BookedAt < PendingBookingExpirationWindow)
					return Result.Failure("Booking cannot be expired before 2 hours from request time");

				var now = DateTime.UtcNow;
				booking.Status = BookingStatus.Expired;
				booking.CancelledAt = now;
				booking.UpdatedAt = now;

				_unitOfWork.RideBookings.Update(booking);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Booking {BookingId} expired after pending window", bookingId);
				return Result.Success("Booking expired successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while expiring booking {BookingId}", bookingId);
				return Result.Failure("Unexpected error happened while expiring booking");
			}
		}

		public async Task<Result> CancelBooking(int bookingId, BookingCancellationType cancellationType)
		{
			if (bookingId <= 0)
				return Result.Failure("Valid booking id is required");

			try
			{
				var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
					x => x.Id == bookingId && !x.IsDeleted,
					x => x.Ride,
					x => x.Ride!.DriverProfile);

				if (booking == null)
					return Result.Failure("Booking not found");

				if (booking.Status == BookingStatus.CancelledByRider || booking.Status == BookingStatus.CancelledByDriver || booking.Status == BookingStatus.Expired)
					return Result.Failure("Booking is already cancelled or expired");

				var shouldRestoreSeats = booking.Status == BookingStatus.Accepted;
				var now = DateTime.UtcNow;
				booking.Status = cancellationType == BookingCancellationType.Rider
					? BookingStatus.CancelledByRider
					: BookingStatus.CancelledByDriver;
				booking.CancelledAt = now;
				booking.UpdatedAt = now;

				if (shouldRestoreSeats && booking.Ride != null)
				{
					booking.Ride.AvailableSeats = Math.Min(booking.Ride.TotalSeats, booking.Ride.AvailableSeats + booking.NumOfSeats);
					booking.Ride.UpdatedAt = now;
					_unitOfWork.Rides.Update(booking.Ride);
				}

				_unitOfWork.RideBookings.Update(booking);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Booking {BookingId} cancelled by {CancellationType}", bookingId, cancellationType);
				return Result.Success("Booking cancelled successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while cancelling booking {BookingId}", bookingId);
				return Result.Failure("Unexpected error happened while cancelling booking");
			}
		}

		private static PaginatedResponse<T> BuildPaginatedResponse<T>(List<T> items, int page, int pageSize, int totalCount)
		{

			return new PaginatedResponse<T>
			{
				Page = page,
				PageSize = pageSize,
				TotalCount = totalCount,
				TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
				Items = items
			};
		}

		private static BookingStatus? MapQueryStatus(BookingStatusQueryEnum? filterBy)
		{
			if (!filterBy.HasValue)
				return null;

			return filterBy.Value switch
			{
				BookingStatusQueryEnum.Pending => BookingStatus.Pending,
				BookingStatusQueryEnum.Accepted => BookingStatus.Accepted,
				BookingStatusQueryEnum.CancelledByRider => BookingStatus.CancelledByRider,
				BookingStatusQueryEnum.CancelledByDriver => BookingStatus.CancelledByDriver,
				BookingStatusQueryEnum.Expired => BookingStatus.Expired,
				_ => null
			};
		}

		private static BookingStatus? MapUpcomingQueryStatus(UpcomingTripStatusQueryEnum? filterBy)
		{
			if (!filterBy.HasValue)
				return null;

			return filterBy.Value switch
			{
				UpcomingTripStatusQueryEnum.Pending => BookingStatus.Pending,
				UpcomingTripStatusQueryEnum.Accepted => BookingStatus.Accepted,
				_ => null
			};
		}

		private static BookingStatus? MapPastQueryStatus(PastTripStatusQueryEnum? filterBy)
		{
			if (!filterBy.HasValue)
				return null;

			return filterBy.Value switch
			{
				PastTripStatusQueryEnum.CancelledByRider => BookingStatus.CancelledByRider,
				PastTripStatusQueryEnum.CancelledByDriver => BookingStatus.CancelledByDriver,
				PastTripStatusQueryEnum.Expired => BookingStatus.Expired,
				_ => null
			};
		}


	}
}
