using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Ban
{
	public class BanHistoryItemDto
	{
		public DateTime From { get; set; }
		public DateTime? Until { get; set; }
		public ReportReason Type { get; set; } 
		public string? Description { get; set; }
		public int DurationDays => Until.HasValue ? (int)(Until.Value - From).TotalDays : 0;
	}
}
