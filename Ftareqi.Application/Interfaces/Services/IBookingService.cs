using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Bookings;
using Ftareqi.Domain.Models;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IBookingService
	{
		/// <summary>
		/// Retrieves a specific booking by ID.
		/// Used when: User or driver views booking details, managing individual booking operations
		/// </summary>
		Task<Result<UserTripRequestResponseDto>> GetBookingById(int bookingId);

		/// <summary>
		/// Creates a new booking request in the current unit of work.
		/// Used when: Orchestrator manages a multi-step transaction and calls SaveChanges/Commit once.
		/// </summary>
		Task<Result<RideBooking>> CreateBooking(CreateBookingRequestDto request, string userId);

		/// <summary>
		/// Accepts a pending booking request by the authenticated driver.
		/// Used when: Driver approves a rider's seat request and seats should be reserved.
		/// </summary>
		Task<Result> AcceptBooking(int bookingId, string driverUserId);

		/// <summary>
		/// Expires a pending booking request if it stays pending for 2+ hours.
		/// Used when: Background job scans pending requests and marks timed-out ones as expired
		/// </summary>
		Task<Result> ExpireBooking(int bookingId);

		/// <summary>
		/// Cancels an existing booking request in the current unit of work.
		/// Used when: Orchestrator coordinates cancellation with wallet release in one transaction.
		/// </summary>
		Task<Result> CancelBooking(int bookingId, BookingCancellationType cancellationType);

		// Driver requests cluster

		/// <summary>
		/// Retrieves incoming booking requests for the authenticated driver with rider and fare details.
		/// Used when: Driver opens real-time trip requests screen to review rider id/name, seats, total money, status and request date
		/// </summary>
		Task<Result<PaginatedResponse<DriverTripRequestResponseDto>>> GetDriverTripRequests(GetTripsRequestsDto request, string driverUserId);

		// User trips cluster

		/// <summary>
		/// Retrieves upcoming trip requests/bookings for the authenticated user.
		/// Used when: User opens 'My Trips > Upcoming' tab to see confirmed/pending future trips
		/// </summary>
		Task<Result<PaginatedResponse<UserTripRequestResponseDto>>> GetUserUpcomingTripRequests(GetUpcomingTripsRequestsDto request, string userId);

		/// <summary>
		/// Retrieves past trip requests/bookings for the authenticated user.
		/// Used when: User opens 'My Trips > Past Trips' tab to see completed/CancelledByDriver history
		/// </summary>
		Task<Result<PaginatedResponse<UserTripRequestResponseDto>>> GetUserPastTripRequests(GenericQueryReq request, string userId);
	}
}
