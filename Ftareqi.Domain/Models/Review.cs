using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class Review
	{
		public int Id { get; set; }
		public string? TextReview { get; set; }
		public float Stars { get; set; }
		public int RideBookingId { get; set; }
		public RideBooking RideBooking { get; set; }
		public int DriverProfileId { get; set; }
		public DriverProfile DriverProfile { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}
}
