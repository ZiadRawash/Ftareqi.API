using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ftareqi.Domain.Enums;

namespace Ftareqi.Application.DTOs.Rides
{
	public class DriverPastRidesResponse
	{
		public double StartLatitude { get; set; }
		public double StartLongitude { get; set; }
		public double EndLatitude { get; set; }
		public double EndLongitude { get; set; }
		public DateTime DepartureTime { get; set; }
		public RideStatus Status { get; set; }
		public int TakenSeats { get; set; }
		public decimal TotalEarnings { get; set; }
		public float AverageRating { get; set; }
	}
}
