using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.Report;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Constants;
using Ftareqi.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ftareqi.API.Controllers
{
	[Authorize]
	[ApiController]
	[Route("api/reports")]
	public class ReportsController : ControllerBase
	{
		/// <summary>
		/// API endpoints to manage user reports and moderation actions.
		/// </summary>
		/// <remarks>
		/// Routes are rooted at <c>/api/reports</c>.
		/// </remarks>
		private readonly IReportService _reportService;

		public ReportsController(IReportService reportService)
		{
			_reportService = reportService;
		}

		/// <summary>
		/// Submit a new report about a user.
		/// </summary>
		/// <param name="request">Request payload with report details.</param>
		/// <returns>Standard API response indicating success or failure.</returns>
		[HttpPost]
		public async Task<ActionResult<ApiResponse>> CreateReport([FromBody] CreateReportDto request)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState.ToApiResponse());
			}

			var reporterUserId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(reporterUserId))
			{
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });
			}

			var result = await _reportService.CreateReport(request, reporterUserId);
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
		/// Get a report by id. Moderators can view any report; reporters/reported users can view their own reports.
		/// </summary>
		/// <param name="reportId">Report identifier.</param>
		/// <returns>Report details.</returns>
		[HttpGet("{reportId:int}")]
		public async Task<ActionResult<ApiResponse<GetReportDto>>> GetById(int reportId)
		{
			var userId = User.GetUserId();
			if (string.IsNullOrWhiteSpace(userId))
			{
				return Unauthorized(new ApiResponse { Success = false, Message = "Unauthorized" });
			}

			var result = await _reportService.GetReportById(reportId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			var isModerator = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Moderator);
			if (!isModerator && result.Data != null &&
				result.Data.ReporterUserId != userId &&
				result.Data.ReportedUserId != userId)
			{
				return Forbid();
			}

			return Ok(new ApiResponse<GetReportDto>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Retrieve paginated reports for moderation.
		/// </summary>
		/// <param name="request">Search and paging parameters.</param>
		/// <returns>Paginated list of reports for moderation.</returns>
		[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
		[HttpGet("moderation/reports")]
		public async Task<ActionResult<ApiResponse<PaginatedResponse<GetReportDto>>>> GetForModeration([FromQuery] SearchReportsDto request)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState.ToApiResponse());
			}

			var result = await _reportService.GetModerationReports(request);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<PaginatedResponse<GetReportDto>>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Get quick summary counts for the moderation dashboard.
		/// </summary>
		/// <returns>Counts including pending reports, today's reports, and distinct reported users.</returns>
		[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
		[HttpGet("moderation/reports/summary")]
		public async Task<ActionResult<ApiResponse<ReportModerationSummaryDto>>> GetModerationSummary()
		{
			var result = await _reportService.GetModerationSummary();
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}
			return Ok(new ApiResponse<ReportModerationSummaryDto>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Get reports related to a specific reported user.
		/// </summary>
		/// <param name="reportedUserId">Id of the reported user.</param>
		/// <param name="request">Paging and filter options.</param>
		/// <returns>Reports for the reported user (paged).</returns>
		[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
		[HttpGet("moderation/reported-user/{reportedUserId}")]
		public async Task<ActionResult<ApiResponse<ReportedUserReportsResponseDto>>> GetForReportedUser(
			string reportedUserId,
			[FromQuery] SearchReportsDto request)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState.ToApiResponse());
			}

			
			var result = await _reportService.GetReportsForReportedUser(request, reportedUserId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<ReportedUserReportsResponseDto>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Update the status of an existing report (e.g., mark as resolved or dismissed).
		/// </summary>
		/// <param name="reportId">Report identifier.</param>
		/// <param name="request">Status update payload.</param>
		/// <returns>Standard API response indicating success or failure.</returns>
		[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
		[HttpPut("{reportId:int}/status")]
		public async Task<ActionResult<ApiResponse>> UpdateStatus(int reportId, [FromBody] UpdateReportStatusDto request)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState.ToApiResponse());
			}

			var result = await _reportService.UpdateReportStatus(reportId, request);
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