using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ftareqi.Domain.Enums;

namespace Ftareqi.Application.DTOs.Rides
{
	public class DriverUpcomingRidesResponse
	{
		public int RideId { get; set; }
		public double StartLatitude { get; set; }
		public double StartLongitude { get; set; }
		public double EndLatitude { get; set; }
		public double EndLongitude { get; set; }
		public int TotalSeats { get; set; }
		public int AvailableSeats { get; set; }
		public decimal PricePerSeat { get; set; }
		public RideStatus Status { get; set; }
		public int WaitingTimeMinutes { get; set; }
		public DateTime DepartureTime { get; set; }
	}
}
