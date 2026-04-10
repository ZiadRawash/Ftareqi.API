using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs.Bookings;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Ftareqi.Persistence.Repositories
{
	public class RideBookingRepository : BaseRepository<RideBooking>, IRideBookingRepository
	{
		private readonly ApplicationDbContext _context;

		public RideBookingRepository(ApplicationDbContext context) : base(context)
		{
			_context = context;
		}

		public async Task<UserTripRequestResponseDto?> GetBookingByIdAsync(int bookingId)
		{
			return await _context.RideBookings
				.AsNoTracking()
				.Where(x => x.Id == bookingId && !x.IsDeleted)
				.Select(x => new UserTripRequestResponseDto
				{
					BookingId = x.Id,
					RideId = x.RideId,
					Status = x.Status,
					BookedAt = x.BookedAt,
					DepartureTime = x.Ride.DepartureTime,
					Seats = x.NumOfSeats,
					TotalAmount = x.NumOfSeats * x.Ride.PricePerSeat,
					DriverName = x.Ride.DriverProfile.User != null ? x.Ride.DriverProfile.User.FullName : string.Empty,
					DriverUserId = x.Ride.DriverProfile.UserId,
					StartLatitude = x.Ride.StartLocation.Y,
					StartLongitude = x.Ride.StartLocation.X,
					EndLatitude = x.Ride.EndLocation.Y,
					EndLongitude = x.Ride.EndLocation.X,
					DriverImg = x.Ride.DriverProfile.Images
						.Where(img => img.Type == ImageType.DriverProfilePhoto)
						.Select(img => img.Url)
						.FirstOrDefault()
				})
				.FirstOrDefaultAsync();
		}

		public async Task<(IReadOnlyList<UserTripRequestResponseDto> Items, int TotalCount)> GetUserUpcomingTripRequestsAsync(
			GetUpcomingTripsRequestsDto request,
			string userId,
			BookingStatus? statusFilter,
			DateTime now)
		{
			var baseQuery = _context.RideBookings
				.AsNoTracking()
				.Where(x =>
					!x.IsDeleted &&
					x.UserId == userId &&
					x.Ride.DepartureTime >= now &&
					(x.Status == BookingStatus.Pending || x.Status == BookingStatus.Accepted) &&
					(!statusFilter.HasValue || x.Status == statusFilter.Value));

			var totalCount = await baseQuery.CountAsync();

			var orderedQuery = request.SortDescending
				? baseQuery.OrderByDescending(x => x.BookedAt)
				: baseQuery.OrderBy(x => x.BookedAt);

			var items = await orderedQuery
				.Skip((request.Page - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(x => new UserTripRequestResponseDto
				{
					BookingId = x.Id,
					RideId = x.RideId,
					Status = x.Status,
					BookedAt = x.BookedAt,
					DepartureTime = x.Ride.DepartureTime,
					Seats = x.NumOfSeats,
					TotalAmount = x.NumOfSeats * x.Ride.PricePerSeat,
					DriverName = x.Ride.DriverProfile.User != null ? x.Ride.DriverProfile.User.FullName : string.Empty,
					DriverUserId = x.Ride.DriverProfile.UserId,
					StartLatitude = x.Ride.StartLocation.Y,
					StartLongitude = x.Ride.StartLocation.X,
					EndLatitude = x.Ride.EndLocation.Y,
					EndLongitude = x.Ride.EndLocation.X,
					DriverImg = x.Ride.DriverProfile.Images
						.Where(img => img.Type == ImageType.DriverProfilePhoto)
						.Select(img => img.Url)
						.FirstOrDefault()
				})
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<(IReadOnlyList<UserTripRequestResponseDto> Items, int TotalCount)> GetUserPastTripRequestsAsync(
			GenericQueryReq request,
			string userId,
			DateTime now)
		{
			var baseQuery = _context.RideBookings
				.AsNoTracking()
				.Where(x =>
					!x.IsDeleted &&
					x.UserId == userId &&
					x.Ride.DepartureTime < now &&
					(x.Status == BookingStatus.Accepted || x.Status == BookingStatus.CancelledByDriver));

			var totalCount = await baseQuery.CountAsync();

			var orderedQuery = request.SortDescending
				? baseQuery.OrderByDescending(x => x.BookedAt)
				: baseQuery.OrderBy(x => x.BookedAt);

			var items = await orderedQuery
				.Skip((request.Page - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(x => new UserTripRequestResponseDto
				{
					BookingId = x.Id,
					RideId = x.RideId,
					Status = x.Status,
					BookedAt = x.BookedAt,
					DepartureTime = x.Ride.DepartureTime,
					Seats = x.NumOfSeats,
					TotalAmount = x.NumOfSeats * x.Ride.PricePerSeat,
					DriverName = x.Ride.DriverProfile.User != null ? x.Ride.DriverProfile.User.FullName : string.Empty,
					DriverUserId = x.Ride.DriverProfile.UserId,
					StartLatitude = x.Ride.StartLocation.Y,
					StartLongitude = x.Ride.StartLocation.X,
					EndLatitude = x.Ride.EndLocation.Y,
					EndLongitude = x.Ride.EndLocation.X,
					DriverImg = x.Ride.DriverProfile.Images
						.Where(img => img.Type == ImageType.DriverProfilePhoto)
						.Select(img => img.Url)
						.FirstOrDefault()
				})
				.ToListAsync();

			return (items, totalCount);
		}
	}
}