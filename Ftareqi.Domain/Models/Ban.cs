using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ftareqi.Domain.Models
{
	public class Ban
	{
		public int Id { get; set; }
		public int DriverProfileId { get; set; }
		public DriverProfile DriverProfile { get; set; } = null!;
		public string BannedByUserId { get; set; } = null!;
		public User BannedByUser { get; set; } = null!;
		public ReportReason BanReason { get; set; }
		public DateTime BannedFrom { get; set; } = DateTime.UtcNow;
		public DateTime? BannedUntil { get; set; }
		[NotMapped]
		public bool IsActive => !BannedUntil.HasValue || BannedUntil > DateTime.UtcNow;
		public string? Description { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }
	}
}