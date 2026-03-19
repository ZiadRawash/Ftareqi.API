using Ftareqi.Domain.Enums;

namespace Ftareqi.Application.DTOs.Rides
{
	public class RideSearchResponseDto
	{
		public int RideId { get; set; }
		public int DriverProfileId { get; set; }
		public double StartLatitude { get; set; }
		public double StartLongitude { get; set; }
		public double EndLatitude { get; set; }
		public double EndLongitude { get; set; }
		public DateTime DepartureTime { get; set; }
		public int AvailableSeats { get; set; }
		public decimal PricePerSeat { get; set; }
		public RideStatus Status { get; set; }
		public double? DriverRate { get; set; }
		//public double DistanceFromStartInMeters { get; set; }
		//public double DistanceToEndInMeters { get; set; }
	}
}