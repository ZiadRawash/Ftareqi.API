using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Ban
{
	public class DriverBanHistoryDto
	{
		public int DriverProfileId { get; set; }
		public int TotalBannedDays { get; set; }
		public DateTime? LatestBannedUntil { get; set; }
		public List<BanHistoryItemDto> History { get; set; } = new();
	}
}
