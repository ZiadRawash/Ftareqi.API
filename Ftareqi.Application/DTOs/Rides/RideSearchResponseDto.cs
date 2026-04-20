using Ftareqi.Domain.Enums;
using System.Globalization;

namespace Ftareqi.Application.DTOs.Rides
{
	public class RideSearchResponseDto
	{
		public int RideId { get; set; }
		public string DriverUserId { get; set; }
		public string StartAddress { get; set; } = string.Empty;
		public string EndAddress { get; set; } = string.Empty;
		public double StartLatitude { get; set; }
		public double StartLongitude { get; set; }
		public double EndLatitude { get; set; }
		public double EndLongitude { get; set; }
		public DateTime DepartureTime { get; set; }
		public int AvailableSeats { get; set; }
		public decimal PricePerSeat { get; set; }
		public RideStatus Status { get; set; }
		public double? DriverRate { get; set; }
		public string DriverName { get; set; }= string.Empty;
		public string DriverImgUrl { get; set; }= string.Empty;
		
	}
}