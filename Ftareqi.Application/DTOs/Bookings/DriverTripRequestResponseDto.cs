using Ftareqi.Domain.Enums;

namespace Ftareqi.Application.DTOs.Bookings
{
	public class DriverTripRequestResponseDto
	{
		public int BookingId { get; set; }
		public int RideId { get; set; }
		public string RiderUserId { get; set; } = string.Empty;
		public string RiderName { get; set; } = string.Empty;
		public int RequestedSeats { get; set; }
		public decimal TotalAmount { get; set; }
		public BookingStatus Status { get; set; }
		public DateTime RequestedAt { get; set; }
		public DateTime DepartureTime { get; set; }
		public string StartAddress { get; set; } = string.Empty;
		public string EndAddress { get; set; } = string.Empty;
	}
}
