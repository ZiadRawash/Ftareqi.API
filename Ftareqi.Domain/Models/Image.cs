using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class Image
	{
		public  int Id { get; set; }
		public string Url { get; set; } = default!;
		public string PublicId { get; set; } = default!;
		public ImageType Type { get; set; }
		public bool IsDeleted { get; set; } = false;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public User? User { get; set; }

		[ForeignKey(nameof(User))]
		public string? UserId { get; set; }

		public DriverProfile? DriverProfile { get; set; }
		[ForeignKey(nameof(DriverProfile))]
		public int? DriverProfileId { get; set; }

	}
}
