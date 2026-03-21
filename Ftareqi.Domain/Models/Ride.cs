using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ftareqi.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Ftareqi.Domain.Models
{
	public class Ride
	{
		public int Id { get; set; }
		public Point StartLocation { get; set; } = null!;
		public Point EndLocation { get; set; } = null!;
		public DateTime DepartureTime { get; set; }
		public int TotalSeats { get; set; }
		public int AvailableSeats { get; set; }
		public decimal PricePerSeat { get; set; }
		public TimeSpan WaitingTime { get; set; }
		public RideStatus Status { get; set; } = RideStatus.Scheduled;
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public RidePreferences RidePreferences { get; set; } = null!;
		public DriverProfile DriverProfile { get; set; } = null!;
		public int DriverProfileId { get; set; }
		public ICollection<RideBooking> RideBookings { get; set; } = new List<RideBooking>();
	}
}
