using Ftareqi.Domain.Enums;

namespace Ftareqi.Application.DTOs.Report
{
	public class GetReportDto
	{
		public int Id { get; set; }
		public string ReporterUserId { get; set; } = string.Empty;
		public string ReporterName { get; set; } = string.Empty;
		public string ReportedUserId { get; set; } = string.Empty;
		public string ReportedUserName { get; set; } = string.Empty;
		public ReportReason Type { get; set; }
		public ReportStatus Status { get; set; }
		public string? Description { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}
}