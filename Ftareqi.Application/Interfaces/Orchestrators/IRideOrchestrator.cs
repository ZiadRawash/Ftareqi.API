using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Bookings;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Ftareqi.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Orchestrators
{
	/// <summary>
	/// Coordinates multi-step ride booking workflows that span booking, wallet, and notifications.
	/// </summary>
	public interface IRideOrchestrator
	{
		/// <summary>
		/// Creates a ride booking request for a rider and handles related orchestration concerns.
		/// </summary>
		/// <param name="model">Booking request payload containing ride and seat details.</param>
		/// <param name="userId">Authenticated rider user id.</param>
		/// <returns>A result indicating whether the booking request workflow succeeded.</returns>
		public Task<Result> CreateRideBookingRequest(CreateBookingRequestDto model, string userId);

		/// <summary>
		/// Accepts a pending booking request by the ride owner (driver).
		/// </summary>
		/// <param name="bookingId">Target booking id.</param>
		/// <param name="driverId">Authenticated driver user id.</param>
		/// <returns>A result indicating whether the accept workflow succeeded.</returns>
		public Task<Result> AcceptRideBookingRequest(int bookingId, string driverId);

		/// <summary>
		/// Declines a pending booking request by the ride owner (driver).
		/// </summary>
		/// <param name="bookingId">Target booking id.</param>
		/// <param name="driverId">Authenticated driver user id.</param>
		/// <returns>A result indicating whether the decline workflow succeeded.</returns>
		public Task<Result> DeclineRideBookingRequest(int bookingId, string driverId);

		/// <summary>
		/// Cancels a rider's own booking request and applies related orchestration actions.
		/// </summary>
		/// <param name="bookingId">Target booking id.</param>
		/// <param name="riderId">Authenticated rider user id.</param>
		/// <returns>A result indicating whether the cancel workflow succeeded.</returns>
		public Task<Result> CancelRideBookingByRider(int bookingId, string riderId);
		public Task<Result> HandleExpiredBookings();
	}

	}
