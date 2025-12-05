using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class Car
	{
		public int Id { get; set; }	
		public string Model { get; set; } = default!;
		public string Color { get; set; } = default!;
		public string palette { get; set; } = default!;
		public int NumOfSeats { get; set; }
		public bool IsDeleted { get; set; }
		public DateTime CreatedAt { get; set; }= DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; }
		public DateTime LicenseExpiryDate { get; set; }

		public DriverProfile? DriverProfile { get; set; }
		[ForeignKey(nameof(DriverProfile))]
		public  int DriverProfileId { get; set; }
		public ICollection<Image> Images { get; set; } = new List<Image>();

	}
}
