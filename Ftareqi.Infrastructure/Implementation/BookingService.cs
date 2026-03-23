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
		//private static readonly TimeSpan PendingBookingExpirationWindow = TimeSpan.FromHours(2);

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
			{
				_logger.LogWarning("CreateBooking called with null request for user {UserId}", userId);
				return Result<int>.Failure("Booking data is required");
			}

			if (string.IsNullOrWhiteSpace(userId))
			{
				_logger.LogWarning("CreateBooking called with empty user id");
				return Result<int>.Failure("User id is required");
			}

			if (request.RideId <= 0)
			{
				_logger.LogWarning("CreateBooking called with invalid ride id {RideId} for user {UserId}", request.RideId, userId);
				return Result<int>.Failure("Valid ride id is required");
			}

			if (request.NumberOfSeats <= 0)
			{
				_logger.LogWarning("CreateBooking called with non-positive seats {Seats} for user {UserId}", request.NumberOfSeats, userId);
				return Result<int>.Failure("Number of seats must be greater than zero");
			}

			try
			{
				var now = DateTime.UtcNow;
				var ride = await _unitOfWork.Rides.FirstOrDefaultAsync(
					x => x.Id == request.RideId,
					x => x.DriverProfile,
					x => x.RideBookings);

				if (ride == null || ride.DriverProfile == null)
				{
					_logger.LogWarning("CreateBooking: ride {RideId} not found for user {UserId}", request.RideId, userId);
					return Result<int>.Failure("Ride not found");
				}

				if (ride.DriverProfile.IsDeleted)
				{
					_logger.LogWarning("CreateBooking: driver profile {DriverProfileId} for ride {RideId} is deleted", ride.DriverProfileId, ride.Id);
					return Result<int>.Failure("Ride is not available for booking");
				}

				if (ride.DriverProfile.UserId == userId)
				{
					_logger.LogWarning("CreateBooking: user {UserId} attempted to book own ride {RideId}", userId, ride.Id);
					return Result<int>.Failure("You cannot book your own trip");
				}

				if (ride.Status != RideStatus.Scheduled)
				{
					_logger.LogWarning("CreateBooking: ride {RideId} has invalid status {Status} for booking", ride.Id, ride.Status);
					return Result<int>.Failure("Ride is not available for booking");
				}

				if (ride.DepartureTime <= now)
				{
					_logger.LogWarning("CreateBooking: ride {RideId} departure time {DepartureTime} is in the past", ride.Id, ride.DepartureTime);
					return Result<int>.Failure("Cannot create booking request after departure time");
				}

				if (request.NumberOfSeats > ride.AvailableSeats)
				{
					_logger.LogWarning("CreateBooking: requested seats {RequestedSeats} exceed available seats {AvailableSeats} for ride {RideId}", request.NumberOfSeats, ride.AvailableSeats, ride.Id);
					return Result<int>.Failure("Requested seats exceed available seats");
				}

				var hasExistingActiveBooking = await _unitOfWork.RideBookings.ExistsAsync(
					x => x.RideId == request.RideId &&
						 x.UserId == userId &&
						 !x.IsDeleted &&
						 (x.Status == BookingStatus.Pending ||
						  x.Status == BookingStatus.Accepted ));
				if (hasExistingActiveBooking)
					{
						_logger.LogWarning("CreateBooking: user {UserId} already has active booking for ride {RideId}", userId, request.RideId);
						return Result<int>.Failure("You already have an active booking request for this ride");
					}

				var booking = new RideBooking
				{
					RideId = request.RideId,
					UserId = userId,
					NumOfSeats = request.NumberOfSeats,
					Status = BookingStatus.Pending,
					BookedAt = now,
					CancelledAt = now,
					ExpiresAt= now.AddHours(2),
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
			{
				_logger.LogWarning("GetBookingById called with invalid booking id {BookingId}", bookingId);
				return Result<UserTripRequestResponseDto>.Failure("Valid booking id is required");
			}

			try
			{
				var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
					x => x.Id == bookingId && !x.IsDeleted,
					x => x.Ride,
					x => x.Ride!.DriverProfile!,
					x => x.Ride!.DriverProfile!.User!);

				if (booking == null)
				{
					_logger.LogWarning("GetBookingById: booking {BookingId} not found", bookingId);
					return Result<UserTripRequestResponseDto>.Failure("Booking not found");
				}

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
			{
				_logger.LogWarning("GetDriverTripRequests called with null request for driver user {UserId}", driverUserId);
				return Result<PaginatedResponse<DriverTripRequestResponseDto>>.Failure("Request data is required");
			}

			try
			{
				var profile = await _unitOfWork.DriverProfiles.FirstOrDefaultAsNoTrackingAsync(
					x => x.UserId == driverUserId && !x.IsDeleted && x.Status == DriverStatus.Active);

				if (profile == null)
				{
					_logger.LogWarning("GetDriverTripRequests: no active driver profile found for user {UserId}", driverUserId);
					return Result<PaginatedResponse<DriverTripRequestResponseDto>>.Failure("No active driver profile found");
				}

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
			{
				_logger.LogWarning("GetUserUpcomingTripRequests called with null request for user {UserId}", userId);
				return Result<PaginatedResponse<UserTripRequestResponseDto>>.Failure("Request data is required");
			}

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
			{
				_logger.LogWarning("GetUserPastTripRequests called with null request for user {UserId}", userId);
				return Result<PaginatedResponse<UserTripRequestResponseDto>>.Failure("Request data is required");
			}
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
			{
				_logger.LogWarning("ExpireBooking called with invalid booking id {BookingId}", bookingId);
				return Result.Failure("Valid booking id is required");
			}

			try
			{
				var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
					x => x.Id == bookingId &&
						 !x.IsDeleted,
					x => x.Ride);

				if (booking == null)
				{
					_logger.LogWarning("ExpireBooking: booking {BookingId} not found", bookingId);
					return Result.Failure("Booking not found");
				}

				if (booking.Status == BookingStatus.CancelledByRider || booking.Status == BookingStatus.CancelledByDriver || booking.Status == BookingStatus.Expired)
				{
					_logger.LogInformation("ExpireBooking: booking {BookingId} already has terminal status {Status}", bookingId, booking.Status);
					return Result.Failure("Booking is already cancelled or expired");
				}

				if (booking.Status != BookingStatus.Pending)
				{
					_logger.LogWarning("ExpireBooking: booking {BookingId} has non-pending status {Status}", bookingId, booking.Status);
					return Result.Failure("Only pending bookings can be expired");
				}


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

		public async Task<Result> AcceptBooking(int bookingId, string driverUserId)
		{			
			if (string.IsNullOrWhiteSpace(driverUserId))
			{
				_logger.LogWarning("AcceptBooking called with empty driver user id for booking {BookingId}", bookingId);
				return Result.Failure("Driver user id is required");
			}

			try
			{
				var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
					x => x.Id == bookingId && !x.IsDeleted,
					x => x.Ride,
					x => x.Ride!.DriverProfile);
				if (booking == null)
				{
					_logger.LogWarning("AcceptBooking: booking {BookingId} not found for driver user {DriverUserId}", bookingId, driverUserId);
					return Result.Failure("Booking not found");
				}

				if (booking.Ride == null || booking.Ride.DriverProfile == null)
				{
					_logger.LogWarning("AcceptBooking: ride or driver profile missing for booking {BookingId}", bookingId);
					return Result.Failure("Ride not found for this booking");
				}

				if (booking.Ride.DriverProfile.UserId != driverUserId)
				{
					_logger.LogWarning("AcceptBooking: unauthorized driver user {DriverUserId} for booking {BookingId}", driverUserId, bookingId);
					return Result.Failure("You are not authorized to accept this booking");
				}

				if (booking.Status == BookingStatus.CancelledByRider ||
					booking.Status == BookingStatus.CancelledByDriver ||
					booking.Status == BookingStatus.Expired)
				{
					_logger.LogInformation("AcceptBooking: booking {BookingId} already has terminal status {Status}", bookingId, booking.Status);
					return Result.Failure("Booking is already cancelled or expired");
				}

				if (booking.Status == BookingStatus.Accepted)
				{
					_logger.LogInformation("AcceptBooking: booking {BookingId} already accepted", bookingId);
					return Result.Failure("Booking is already accepted");
				}

				if (booking.Status != BookingStatus.Pending)
				{
					_logger.LogWarning("AcceptBooking: booking {BookingId} has non-pending status {Status}", bookingId, booking.Status);
					return Result.Failure("Only pending bookings can be accepted");
				}

				var now = DateTime.UtcNow;
				if (booking.ExpiresAt <= now)
				{
					_logger.LogInformation("AcceptBooking: booking {BookingId} has expired at {ExpiresAt}", bookingId, booking.ExpiresAt);
					return Result.Failure("Booking has already expired");
				}

				if (booking.Ride.Status != RideStatus.Scheduled)
				{
					_logger.LogWarning("AcceptBooking: ride {RideId} has invalid status {Status}", booking.Ride.Id, booking.Ride.Status);
					return Result.Failure("Ride is not available for booking");
				}

				if (booking.Ride.DepartureTime <= now)
				{
					_logger.LogWarning("AcceptBooking: ride {RideId} departure time {DepartureTime} is in the past", booking.Ride.Id, booking.Ride.DepartureTime);
					return Result.Failure("Cannot accept booking after departure time");
				}

				if (booking.NumOfSeats > booking.Ride.AvailableSeats)
				{
					_logger.LogWarning("AcceptBooking: requested seats {RequestedSeats} exceed available seats {AvailableSeats} for ride {RideId}", booking.NumOfSeats, booking.Ride.AvailableSeats, booking.Ride.Id);
					return Result.Failure("Not enough available seats to accept this booking");
				}

				booking.Status = BookingStatus.Accepted;
				booking.UpdatedAt = now;
				booking.Ride.AvailableSeats -= booking.NumOfSeats;
				booking.Ride.UpdatedAt = now;
				_unitOfWork.RideBookings.Update(booking);
				_unitOfWork.Rides.Update(booking.Ride);
				await _unitOfWork.SaveChangesAsync();
				_logger.LogInformation("Booking {BookingId} accepted by driver user {DriverUserId}", bookingId, driverUserId);
				return Result.Success("Booking accepted successfully");
			}
			catch (Exception ex)
			{
				_logger.LogCritical(ex, "Unexpected error while accepting booking {BookingId} by driver {DriverUserId}", bookingId, driverUserId);
				return Result.Failure("Unexpected error happened while accepting booking");
			}
		}

		public async Task<Result> CancelBooking(int bookingId, BookingCancellationType cancellationType)
		{
			if (bookingId <= 0)
			{
				_logger.LogWarning("CancelBooking called with invalid booking id {BookingId}", bookingId);
				return Result.Failure("Valid booking id is required");
			}

			try
			{
				var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
					x => x.Id == bookingId && !x.IsDeleted,
					x => x.Ride,
					x => x.Ride!.DriverProfile);

				if (booking == null)
				{
					_logger.LogWarning("CancelBooking: booking {BookingId} not found", bookingId);
					return Result.Failure("Booking not found");
				}

				if (booking.Status == BookingStatus.CancelledByRider || booking.Status == BookingStatus.CancelledByDriver || booking.Status == BookingStatus.Expired)
				{
					_logger.LogInformation("CancelBooking: booking {BookingId} already has terminal status {Status}", bookingId, booking.Status);
					return Result.Failure("Booking is already cancelled or expired");
				}

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
