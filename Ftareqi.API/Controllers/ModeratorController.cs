using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ftareqi.API.Controllers
{
	[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
	[Route("api/moderator/driver-requests")]
	[ApiController]
	public class ModeratorController : ControllerBase
	{
		private readonly IDriverOrchestrator _driverOrchestrator;

		public ModeratorController(IDriverOrchestrator driverOrchestrator)
		{
			_driverOrchestrator = driverOrchestrator;
		}

		// GET: api/moderator/driver-requests/pending

		[HttpGet("pending")]
		public async Task<ActionResult<ApiResponse<PaginatedResponse<DriverProfileWithUsernameDto>>>> GetPendingProfiles([FromQuery] GenericQueryReq model)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage)
					.ToList();

				return BadRequest(new ApiResponse<PaginatedResponse<DriverProfileWithUsernameDto>>
				{
					Success = false,
					Errors = errors,
					Message = "Invalid request data"
				});
			}

			var response = await _driverOrchestrator.GetPendingDriverProfiles(model);
			return Ok(new ApiResponse<PaginatedResponse<DriverProfileWithUsernameDto>>
			{
				Data = response.Data,
				Errors = response.Errors,
				Message = response.Message,
				Success = response.IsSuccess,
			});
		}

		// GET: api/moderator/driver-requests/{userId}
		[HttpGet("{driverId}")]
		public async Task<ActionResult<ApiResponse<DriverWithCarResponseDto>>> GetDriverDetails([FromRoute] int driverId)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage)
					.ToList();

				return BadRequest(new ApiResponse<DriverWithCarResponseDto>
				{
					Success = false,
					Errors = errors,
					Message = "Invalid request data"
				});
			}

			var response = await _driverOrchestrator.GetDriverDetails(driverId);
			if (response.IsFailure)
			{
				return BadRequest(new ApiResponse<DriverWithCarResponseDto>
				{
					Errors = response.Errors,
					Message = response.Message,
					Success = response.IsSuccess
				});
			}

			return Ok(new ApiResponse<DriverWithCarResponseDto>
			{
				Data = response.Data,
				Errors = response.Errors,
				Message = response.Message,
				Success = response.IsSuccess
			});
		}

		// POST: api/moderator/driver-requests/{driverId}/approve
		[HttpPost("{driverId}/approve")]
		public async Task<ActionResult<ApiResponse>> ApproveDriverRequest([FromRoute] int driverId)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage)
					.ToList();

				return BadRequest(new ApiResponse
				{
					Success = false,
					Errors = errors,
					Message = "Invalid request data"
				});
			}

			var result = await _driverOrchestrator.ApproveDriverProfile(driverId);
			if (result.IsFailure)
				return BadRequest(new ApiResponse { Errors = result.Errors, Message = result.Message, Success = false });

			return Ok(new ApiResponse { Errors = result.Errors, Message = result.Message, Success = true });
		}

		// POST: api/moderator/driver-requests/{driverId}/reject
		[HttpPost("{driverId}/reject")]
		public async Task<ActionResult<ApiResponse>> RejectDriverRequest([FromRoute] int driverId)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage)
					.ToList();

				return BadRequest(new ApiResponse
				{
					Success = false,
					Errors = errors,
					Message = "Invalid request data"
				});
			}
			var result = await _driverOrchestrator.RejectDriverProfile(driverId);
			if (result.IsFailure)
				return BadRequest(new ApiResponse { Errors = result.Errors, Message = result.Message, Success = false });

			return Ok(new ApiResponse { Errors = result.Errors, Message = result.Message, Success = true });
		}
	}
}
