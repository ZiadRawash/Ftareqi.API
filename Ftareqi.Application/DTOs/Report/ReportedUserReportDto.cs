using Ftareqi.Domain.Enums;

namespace Ftareqi.Application.DTOs.Report
{
	public class ReportedUserReportDto
	{
		public string ReporterName { get; set; } = string.Empty;
		public ReportReason Type { get; set; }
		public ReportStatus Status { get; set; }
		public string? Description { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}
}