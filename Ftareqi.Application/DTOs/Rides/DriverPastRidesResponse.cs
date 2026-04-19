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
		public int RideId { get; set; }
		public double StartLatitude { get; set; }
		public double StartLongitude { get; set; }
		public string StartAddress { get; set; } = string.Empty;
		public double EndLatitude { get; set; }
		public double EndLongitude { get; set; }
		public string EndAddress { get; set; } = string.Empty;
		public DateTime DepartureTime { get; set; }
		public RideStatus Status { get; set; }
		public int TakenSeats { get; set; }
		public decimal TotalEarnings { get; set; }
		public float AverageRating { get; set; }
	}
}
