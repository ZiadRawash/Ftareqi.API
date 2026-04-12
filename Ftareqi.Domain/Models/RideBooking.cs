using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class RideBooking
	{
		public int Id { get; set; }
		public int NumOfSeats { get; set; }
		public DateTime BookedAt { get; set; }
		public DateTime ExpiresAt { get; set; }
		public DateTime CancelledAt { get; set; }
		public bool IsDeleted { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }

		public User User { get; set; }
		public string UserId { get; set; }
		public Ride Ride { get; set; }
		public int RideId { get; set; }
		public Review? Review { get; set; }
		public BookingStatus Status { get; set; } //Pending,Confirmed,CancelledByRider,CancelledByDriver,Accepted,Expired
	}
}
