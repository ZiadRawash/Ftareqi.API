using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.Ban;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ftareqi.API.Controllers
{
	/// <summary>
	/// Controller for managing driver profile bans and retrieving ban-related data.
	/// </summary>
	[ApiController]
	[Route("api/bans")]
	public class BansController : ControllerBase
	{
		private readonly IBanService _banService;

		public BansController(IBanService banService)
		{
			_banService = banService;
		}

		/// <summary>
		/// Bans a specific driver profile.
		/// </summary>
		[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
		[HttpPost("{driverProfileId:int}")]
		public async Task<ActionResult<ApiResponse>> BanDriverProfile(string driverUserId, [FromBody] CreateBanDto request)
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

			var moderatorUserId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(moderatorUserId))
			{
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });
			}

			var result = await _banService.BanDriverProfileAsync(driverUserId, request, moderatorUserId);
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
		/// Lifts a ban for a specific driver profile.
		/// </summary>
		[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
		[HttpPatch("{banId:int}/unban")]
		public async Task<ActionResult<ApiResponse>> UnbanDriverProfile(int banId)
		{
			var result = await _banService.UnbanDriverProfileAsync(banId);
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
		/// Retrieves a statistical summary of all bans.
		/// </summary>
		[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
		[HttpGet("summary")]
		public async Task<ActionResult<ApiResponse<BanSummaryDto>>> GetSummary()
		{
			var result = await _banService.GetSummaryAsync();
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse<BanSummaryDto>
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<BanSummaryDto>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Retrieves a paginated list of currently banned driver profiles.
		/// </summary>
		[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
		[HttpGet]
		public async Task<ActionResult<ApiResponse<PaginatedResponse<BannedProfileDto>>>> GetBannedProfiles([FromQuery] GenericQueryReq request)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage)
					.ToList();

				return BadRequest(new ApiResponse<PaginatedResponse<BannedProfileDto>>
				{
					Success = false,
					Errors = errors,
					Message = "Invalid request data"
				});
			}

			var result = await _banService.GetBannedProfilesAsync(request);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse<PaginatedResponse<BannedProfileDto>>
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<PaginatedResponse<BannedProfileDto>>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Retrieves the ban history for the currently authenticated driver.
		/// </summary>
		[Authorize]
		[HttpGet("history")] // Note: Added "history" to the route to prevent ambiguous HTTP GET matches with GetBannedProfiles.
		public async Task<ActionResult<ApiResponse<DriverBanHistoryDto>>> GetUserBanHistory()
		{
			var userId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(userId))
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });
			var result = await _banService.GetDriverBanHistoryAsync(userId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors,
				});
			}
			return Ok(
				new ApiResponse<DriverBanHistoryDto>
				{
					Success = true,
					Data = result.Data,
					Errors = result.Errors,
					Message = result.Message,
				}
			);
		}
	}
}