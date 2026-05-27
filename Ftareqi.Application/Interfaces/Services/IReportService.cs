using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Report;
using Ftareqi.Domain.Enums;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IReportService
	{
		Task<Result> CreateReport(CreateReportDto model, string reporterUserId);
		Task<Result<GetReportDto>> GetReportById(int reportId);
		Task<Result<PaginatedResponse<GetReportDto>>> GetModerationReports(SearchReportsDto request);
		Task<Result<ReportModerationSummaryDto>> GetModerationSummary();
		Task<Result<ReportedUserReportsResponseDto>> GetReportsForReportedUser(SearchReportsDto request , string reportedUserId);
		Task<Result> UpdateReportStatus(int reportId, UpdateReportStatusDto model);
	}
}