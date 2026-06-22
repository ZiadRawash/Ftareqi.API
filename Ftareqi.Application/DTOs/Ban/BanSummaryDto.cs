using Ftareqi.Domain.Enums;

namespace Ftareqi.Application.DTOs.Ban
{
	public class BanSummaryDto
	{
		public int TotalBannedUsersCount { get; set; }
		public List<BanTypeStatisticDto> Statistics { get; set; } = new List<BanTypeStatisticDto>();
	}

	public class BanTypeStatisticDto
	{
		public ReportReason Type { get; set; }
		public int Count { get; set; }
		public decimal Percentage { get; set; }
	}
}
