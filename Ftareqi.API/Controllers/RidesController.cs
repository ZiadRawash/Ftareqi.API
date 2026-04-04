using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.Rides;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ftareqi.API.Controllers
{
	[Authorize]
	[ApiController]
	[Route("api/rides")]
	public class RidesController : ControllerBase
	{
		private readonly IRideService _rideService;

		public RidesController(IRideService rideService)
		{
			_rideService = rideService;
		}

		/// <summary>
		/// Creates a new ride for the authenticated driver.
		/// </summary>
		[Authorize(Policy = "DriverOnly")]
		[HttpPost]
		public async Task<ActionResult<ApiResponse>> CreateRide([FromBody] CreateRideRequestDto request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var userId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });

			var result = await _rideService.CreateRide(request, userId);
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
		/// Searches available rides for the authenticated user.
		/// </summary>
		[HttpGet("search")]
		public async Task<ActionResult<ApiResponse<PaginatedResponse<RideSearchResponseDto>>>> SearchRides([FromQuery] RideSearchRequestDto request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var userId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });

			var result = await _rideService.SearchForRides(request, userId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<PaginatedResponse<RideSearchResponseDto>>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Gets upcoming rides created by the authenticated driver.
		/// </summary>
		[HttpGet("driver/upcoming")]
		public async Task<ActionResult<ApiResponse<PaginatedResponse<DriverUpcomingRidesResponse>>>> GetDriverUpcomingRides([FromQuery] GenericQueryReq request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var userId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });

			var result = await _rideService.GetDriverUpcomingRides(request, userId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<PaginatedResponse<DriverUpcomingRidesResponse>>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Gets past rides created by the authenticated driver.
		/// </summary>
		[HttpGet("driver/past")]
		public async Task<ActionResult<ApiResponse<PaginatedResponse<DriverPastRidesResponse>>>> GetDriverPastRides([FromQuery] GenericQueryReq request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var userId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });

			var result = await _rideService.GetDriverPastRides(request, userId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<PaginatedResponse<DriverPastRidesResponse>>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}
	}
}