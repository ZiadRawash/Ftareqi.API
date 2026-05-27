using Ftareqi.Application.Common;

namespace Ftareqi.Application.DTOs.Report
{
	public class ReportedUserReportsResponseDto
	{
		public string ReportedUserId { get; set; } = string.Empty;
		public string ReportedUserName { get; set; } = string.Empty;
		public PaginatedResponse<ReportedUserReportDto> Reports { get; set; } = new();
	}
}