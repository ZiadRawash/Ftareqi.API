using Ftareqi.Domain.Enums;

namespace Ftareqi.Application.DTOs.Ban
{
	public class BannedProfileDto
	{
		public int BanId { get; set; }
		public int DriverProfileId { get; set; }
		public string Name { get; set; } = string.Empty;
		public ReportReason Type { get; set; }
		public DateTime? ExpirationDate { get; set; }
		public string ModeratorName { get; set; } = string.Empty;
	}
}
