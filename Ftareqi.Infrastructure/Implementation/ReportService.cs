using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Report;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Mappers;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Ftareqi.Infrastructure.Implementation
{
	public class ReportService : IReportService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<ReportService> _logger;

		public ReportService(IUnitOfWork unitOfWork, ILogger<ReportService> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<Result> CreateReport(CreateReportDto model, string reporterUserId)
		{
			if (model == null)
			{
				return Result.Failure("Report data is required");
			}

			if (string.IsNullOrWhiteSpace(reporterUserId))
			{
				return Result.Failure("Reporter user id is required");
			}

			if (string.IsNullOrWhiteSpace(model.ReportedUserId))
			{
				return Result.Failure("Reported user id is required");
			}

			if (string.Equals(model.ReportedUserId, reporterUserId, StringComparison.Ordinal))
			{
				return Result.Failure("You cannot report yourself");
			}

			var reporter = await _unitOfWork.Users.FirstOrDefaultAsNoTrackingAsync(x => x.Id == reporterUserId);
			if (reporter == null)
			{
				return Result.Failure("Reporter user not found");
			}

			var reportedUser = await _unitOfWork.Users.FirstOrDefaultAsNoTrackingAsync(x => x.Id == model.ReportedUserId);
			if (reportedUser == null)
			{
				return Result.Failure("Reported user not found");
			}

			var report = new Report
			{
				ReporterUserId = reporterUserId,
				ReportedUserId = model.ReportedUserId,
				Type = model.Type,
				Status = ReportStatus.Pending,
				Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = null
			};

			await _unitOfWork.Reports.AddAsync(report);
			await _unitOfWork.SaveChangesAsync();

			_logger.LogInformation("Report {ReportId} created by user {ReporterUserId} against user {ReportedUserId}", report.Id, reporterUserId, model.ReportedUserId);
			return Result.Success("Report submitted successfully");
		}

		public async Task<Result<GetReportDto>> GetReportById(int reportId)
		{
			if (reportId <= 0)
			{
				return Result<GetReportDto>.Failure("Valid report id is required");
			}

			var report = await _unitOfWork.Reports.FirstOrDefaultAsync(
				x => x.Id == reportId,
				x => x.ReporterUser,
				x => x.ReportedUser);

			if (report == null)
			{
				return Result<GetReportDto>.Failure("Report not found");
			}

			return Result<GetReportDto>.Success(report.ToDto());
		}

		public async Task<Result<PaginatedResponse<GetReportDto>>> GetModerationReports(SearchReportsDto request)
		{
			if (request == null)
			{
				return Result<PaginatedResponse<GetReportDto>>.Failure("Search request is required");
			}

			var (reports, totalCount) = await _unitOfWork.Reports.GetPagedAsync(
				request.Page,
				request.PageSize,
				x => x.CreatedAt,
				x =>(!request.Reason.HasValue || x.Type == request.Reason.Value)
					&& (!request.Status.HasValue || x.Status == request.Status.Value),
				request.SortDescending,
				x => x.ReporterUser,
				x => x.ReportedUser);

			var items = reports.Select(x => x.ToDto()).ToList();
			var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

			return Result<PaginatedResponse<GetReportDto>>.Success(new PaginatedResponse<GetReportDto>
			{
				Items = items,
				Page = request.Page,
				PageSize = request.PageSize,
				TotalCount = totalCount,
				TotalPages = totalPages
			});
		}

		public async Task<Result<ReportModerationSummaryDto>> GetModerationSummary()
		{
			var today = DateTime.UtcNow.Date;
			var tomorrow = today.AddDays(1);
			var pendingReportsCount = await _unitOfWork.Reports.CountAsync(x => x.Status == ReportStatus.Pending);
			var reportsTodayCount = await _unitOfWork.Reports.CountAsync(x => x.CreatedAt >= today && x.CreatedAt < tomorrow);
			var reportedUsersCount = await _unitOfWork.Reports.CountDistinctAsync(x => x.ReportedUserId);

			return Result<ReportModerationSummaryDto>.Success(new ReportModerationSummaryDto
			{
				PendingReportsCount = pendingReportsCount,
				ReportsTodayCount = reportsTodayCount,
				ReportedUsersCount = reportedUsersCount
			});
		}

		public async Task<Result<ReportedUserReportsResponseDto>> GetReportsForReportedUser(SearchReportsDto request, string reportedUserId)
		{
			if (request == null)
			{
				return Result<ReportedUserReportsResponseDto>.Failure("Search request is required");
			}

			if (string.IsNullOrWhiteSpace(reportedUserId))
			{
				return Result<ReportedUserReportsResponseDto>.Failure("Reported user id is required");
			}

			var reportedUser = await _unitOfWork.Users.FirstOrDefaultAsNoTrackingAsync(x => x.Id == reportedUserId);
			if (reportedUser == null)
			{
				return Result<ReportedUserReportsResponseDto>.Failure("Reported user not found");
			}

			var (reports, totalCount) = await _unitOfWork.Reports.GetPagedAsync(
				request.Page,
				request.PageSize,
				x => x.CreatedAt,
				x => x.ReportedUserId == reportedUserId
					&& (!request.Reason.HasValue || x.Type == request.Reason.Value)
					&& (!request.Status.HasValue || x.Status == request.Status.Value),
				request.SortDescending,
				x => x.ReporterUser,
				x => x.ReportedUser);

			var items = reports.Select(x => new ReportedUserReportDto
			{
				ReporterName = x.ReporterUser?.FullName ?? string.Empty,
				Type = x.Type,
				Status = x.Status,
				Description = x.Description,
				CreatedAt = x.CreatedAt,
				UpdatedAt = x.UpdatedAt
			}).ToList();
			var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

			return Result<ReportedUserReportsResponseDto>.Success(new ReportedUserReportsResponseDto
			{
				ReportedUserId = reportedUser.Id,
				ReportedUserName = reportedUser.FullName,
				Reports = new PaginatedResponse<ReportedUserReportDto>
				{
					Items = items,
					Page = request.Page,
					PageSize = request.PageSize,
					TotalCount = totalCount,
					TotalPages = totalPages
				}
			});
		}

		public async Task<Result> UpdateReportStatus(int reportId, UpdateReportStatusDto model)
		{
			if (reportId <= 0)
			{
				return Result.Failure("Valid report id is required");
			}

			if (model == null)
			{
				return Result.Failure("Report status data is required");
			}

			if (model.Status == ReportStatus.Pending)
			{
				return Result.Failure("Report status cannot be set to pending");
			}

			var report = await _unitOfWork.Reports.FirstOrDefaultAsync(x => x.Id == reportId);
			if (report == null)
			{
				return Result.Failure("Report not found");
			}

			report.Status = model.Status;
			report.UpdatedAt = DateTime.UtcNow;
			_unitOfWork.Reports.Update(report);
			await _unitOfWork.SaveChangesAsync();

			return Result.Success("Report status updated successfully");
		}
	}
}