using System.ComponentModel.DataAnnotations;

namespace Ftareqi.Application.DTOs.Bookings
{
	public class CreateBookingRequestDto
	{
		[Required]
		public int RideId { get; set; }
		[Required]
		[Range(1, 10)]
		public int NumberOfSeats { get; set; }

	}
}
