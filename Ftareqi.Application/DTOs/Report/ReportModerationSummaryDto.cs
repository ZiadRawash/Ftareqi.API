namespace Ftareqi.Application.DTOs.Report
{
	public class ReportModerationSummaryDto
	{
		public int PendingReportsCount { get; set; }
		public int ReportsTodayCount { get; set; }
		public int ReportedUsersCount { get; set; }
	}
}