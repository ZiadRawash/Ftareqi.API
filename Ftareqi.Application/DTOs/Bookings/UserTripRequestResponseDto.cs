using Ftareqi.Domain.Enums;

namespace Ftareqi.Application.DTOs.Bookings
{
	public class UserTripRequestResponseDto
	{
		public int BookingId { get; set; }
		public int RideId { get; set; }
		public BookingStatus Status { get; set; }
		public DateTime BookedAt { get; set; }
		public DateTime DepartureTime { get; set; }
		public int Seats { get; set; }
		public decimal TotalAmount { get; set; }
		public string DriverName { get; set; } = string.Empty;
		public string DriverUserId { get; set; } = string.Empty;
		public double StartLatitude { get; set; }
		public double StartLongitude { get; set; }
		public double EndLatitude { get; set; }
		public double EndLongitude { get; set; }
		public string? DriverImg { get; set; }
	}
}
