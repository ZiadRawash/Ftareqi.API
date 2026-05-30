using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs.Bookings;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
			return await BaseQuery()
				.Where(x => x.Id == bookingId)
				.Select(ToDto())
				.FirstOrDefaultAsync();
		}

		public async Task<(IReadOnlyList<UserTripRequestResponseDto> Items, int TotalCount)> GetUserUpcomingTripRequestsAsync(
			GetUpcomingTripsRequestsDto request, string userId, BookingStatus? statusFilter)
		{
			// Upcoming trips now span the full in-progress booking lifecycle, not just Pending/Accepted.
			// This keeps check-in/start progress visible after bookings move to CheckedIn and Started.
			var query = BaseQuery().Where(x =>
				x.UserId == userId &&
				(x.Ride.Status==RideStatus.Scheduled || x.Ride.Status == RideStatus.InProgress|| x.Ride.Status == RideStatus.CheckedIn) &&
				(
					x.Status == BookingStatus.Pending ||
					x.Status == BookingStatus.Accepted ||
					x.Status == BookingStatus.CheckedIn ||
					x.Status == BookingStatus.Started
				) &&
				(!statusFilter.HasValue || x.Status == statusFilter.Value));

			return await ToPagedResult(query, request, includePreferences: true);
		}

		public async Task<(IReadOnlyList<UserTripRequestResponseDto> Items, int TotalCount)> GetUserPastTripRequestsAsync(
			GenericQueryReq request, string userId)
		{
			// Past trips should include completed rides and driver-cancelled rides so they remain visible
			// for user follow-up or reporting, and completed rides now end in BookingStatus.Ended.
			var query = BaseQuery().Where(x =>
				x.UserId == userId &&
				(x.Ride.Status == RideStatus.Cancelled || x.Ride.Status == RideStatus.Completed) &&
				(
					x.Status == BookingStatus.Ended ||
					x.Status == BookingStatus.CancelledByDriver ||
					x.Status == BookingStatus.CancelledByRider
				));

			return await ToPagedResult(query, request);
		}

		private IQueryable<RideBooking> BaseQuery() =>
			_context.RideBookings.AsNoTracking().Where(x => !x.IsDeleted);

		private static async Task<(IReadOnlyList<UserTripRequestResponseDto>, int)> ToPagedResult(
			IQueryable<RideBooking> query, GenericQueryReq request, bool includePreferences = false)
		{
			var total = await query.CountAsync();

			var items = await (request.SortDescending
					? query.OrderByDescending(x => x.BookedAt)
					: query.OrderBy(x => x.BookedAt))
				.Skip((request.Page - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(ToDto(includePreferences))
				.ToListAsync();

			return (items, total);
		}

		private static Expression<Func<RideBooking, UserTripRequestResponseDto>> ToDto(bool includePreferences = false) =>
			x => new UserTripRequestResponseDto
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
				StartAddress = x.Ride.StartAddress,
				EndLatitude = x.Ride.EndLocation.Y,
				EndLongitude = x.Ride.EndLocation.X,
				EndAddress = x.Ride.EndAddress,
				DriverImg = x.Ride.DriverProfile.Images
										 .Where(img => img.Type == ImageType.DriverProfilePhoto)
										 .Select(img => img.Url)
										 .FirstOrDefault(),

				PetsWelcomed = includePreferences && x.Ride.RidePreferences != null && x.Ride.RidePreferences.PetsWelcomed,
				OpenToConversation = includePreferences && x.Ride.RidePreferences != null && x.Ride.RidePreferences.OpenToConversation,
				NoSmoking = includePreferences && x.Ride.RidePreferences != null && x.Ride.RidePreferences.NoSmoking,
				MusicAllowed = includePreferences && x.Ride.RidePreferences != null && x.Ride.RidePreferences.MusicAllowed,
			};
	}
}