using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class Report
	{
		public int Id { get; set; }
		public required string ReporterUserId { get; set; }
		public required string ReportedUserId { get; set; }
		public User ReporterUser { get; set; } = null!;
		public User ReportedUser { get; set; } = null!;
		public ReportReason Type { get; set; }
		public ReportStatus Status { get; set; } = ReportStatus.Pending;
		public string? Description { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }
	}
}
