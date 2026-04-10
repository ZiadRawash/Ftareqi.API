using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs.Bookings;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;

namespace Ftareqi.Application.Interfaces.Repositories
{
	public interface IRideBookingRepository : IBaseRepository<RideBooking>
	{
		Task<UserTripRequestResponseDto?> GetBookingByIdAsync(int bookingId);

		Task<(IReadOnlyList<UserTripRequestResponseDto> Items, int TotalCount)> GetUserUpcomingTripRequestsAsync(
			GetUpcomingTripsRequestsDto request,
			string userId,
			BookingStatus? statusFilter,
			DateTime now);

		Task<(IReadOnlyList<UserTripRequestResponseDto> Items, int TotalCount)> GetUserPastTripRequestsAsync(
			GenericQueryReq request,
			string userId,
			DateTime now);
	}
}