using Ftareqi.Application.Common;

namespace Ftareqi.Application.DTOs.Report
{
	public class ReportModerationResponseDto
	{
		public PaginatedResponse<GetReportDto> Reports { get; set; } = new();
		public ReportModerationSummaryDto Summary { get; set; } = new();
	}
}