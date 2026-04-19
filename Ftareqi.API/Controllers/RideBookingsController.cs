using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.Bookings;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ftareqi.API.Controllers
{
	[Authorize]
	[ApiController]
	[Route("api/ride-bookings")]
	public class RideBookingsController : ControllerBase
	{
		private readonly IBookingService _bookingService;
		private readonly IRideOrchestrator _rideOrchestrator;

		public RideBookingsController(IBookingService bookingService, IRideOrchestrator rideOrchestrator)
		{
			_bookingService = bookingService;
			_rideOrchestrator = rideOrchestrator;
		}

		/// <summary>
		/// Creates a new ride booking request for the authenticated rider.
		/// </summary>
		[HttpPost]
		public async Task<ActionResult<ApiResponse>> CreateBooking([FromBody] CreateBookingRequestDto request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var userId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });

			var result = await _rideOrchestrator.CreateRideBookingRequest(request, userId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors
			});
		}

		/// <summary>
		/// Retrieves booking details by booking id.
		/// </summary>
		[HttpGet("{bookingId:int}")]
		public async Task<ActionResult<ApiResponse<UserTripRequestResponseDto>>> GetBookingById(int bookingId)
		{
			var result = await _bookingService.GetBookingById(bookingId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<UserTripRequestResponseDto>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Gets incoming booking requests for the authenticated driver.
		/// </summary>
		[HttpGet("driver/requests")]
		public async Task<ActionResult<ApiResponse<PaginatedResponse<DriverTripRequestResponseDto>>>> GetDriverRequests([FromQuery] GetTripsRequestsDto request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var driverUserId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(driverUserId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });

			var result = await _bookingService.GetDriverTripRequests(request, driverUserId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<PaginatedResponse<DriverTripRequestResponseDto>>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Gets upcoming bookings for the authenticated rider.
		/// </summary>
		[HttpGet("user/upcoming")]
		public async Task<ActionResult<ApiResponse<PaginatedResponse<UserTripRequestResponseDto>>>> GetUserUpcoming([FromQuery] GetUpcomingTripsRequestsDto request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var userId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });

			var result = await _bookingService.GetUserUpcomingTripRequests(request, userId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<PaginatedResponse<UserTripRequestResponseDto>>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Gets trip history (past bookings) for the authenticated rider.
		/// </summary>
		[HttpGet("user/history")]
		public async Task<ActionResult<ApiResponse<PaginatedResponse<UserTripRequestResponseDto>>>> GetUserTripHistory([FromQuery] GenericQueryReq request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var userId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });

			var result = await _bookingService.GetUserPastTripRequests(request, userId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<PaginatedResponse<UserTripRequestResponseDto>>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Accepts a pending booking by the authenticated driver.
		/// </summary>
		[Authorize(Policy = "DriverOnly")]
		[HttpPost("{bookingId:int}/accept")]
		public async Task<ActionResult<ApiResponse>> AcceptBooking(int bookingId)
		{
			var driverUserId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(driverUserId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });

			var result = await _rideOrchestrator.AcceptRideBookingRequest(bookingId, driverUserId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors
			});
		}

		/// <summary>
		/// Declines a pending booking by the authenticated driver.
		/// </summary>
		[Authorize(Policy = "DriverOnly")]
		[HttpPost("{bookingId:int}/decline")]
		public async Task<ActionResult<ApiResponse>> DeclineBooking(int bookingId)
		{
			var driverUserId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(driverUserId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });

			var result = await _rideOrchestrator.DeclineRideBookingRequest(bookingId, driverUserId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors
			});
		}

		/// <summary>
		/// Cancels a booking by the authenticated rider.
		/// </summary>
		[HttpPost("{bookingId:int}/cancel")]
		public async Task<ActionResult<ApiResponse>> CancelBookingByRider(int bookingId)
		{
			var riderUserId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(riderUserId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });

			var result = await _rideOrchestrator.CancelRideBookingByRider(bookingId, riderUserId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors
			});
		}
	}
}