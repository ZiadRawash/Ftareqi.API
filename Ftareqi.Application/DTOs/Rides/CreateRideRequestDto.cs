using System.ComponentModel.DataAnnotations;

namespace Ftareqi.Application.DTOs.Rides
{
	public class CreateRideRequestDto
	{
		[Required(ErrorMessage = "Start latitude is required.")]
		[Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
		public double StartLatitude { get; set; }

		[Required(ErrorMessage = "Start longitude is required.")]
		[Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
		public double StartLongitude { get; set; }

		[Required(ErrorMessage = "Start address is required.")]
		[StringLength(500, ErrorMessage = "Start address cannot exceed 500 characters.")]
		public string StartAddress { get; set; } = string.Empty;

		[Required(ErrorMessage = "End latitude is required.")]
		[Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
		public double EndLatitude { get; set; }

		[Required(ErrorMessage = "End longitude is required.")]
		[Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
		public double EndLongitude { get; set; }

		[Required(ErrorMessage = "End address is required.")]
		[StringLength(500, ErrorMessage = "End address cannot exceed 500 characters.")]
		public string EndAddress { get; set; } = string.Empty;

		[Required(ErrorMessage = "Departure time is required.")]
		[DataType(DataType.DateTime)]
		public DateTime DepartureTime { get; set; }

		[Required(ErrorMessage = "Total seats count is required.")]
		[Range(1, 5, ErrorMessage = "Total seats must be between 1 and 10.")]
		public int TotalSeats { get; set; }

		[Required(ErrorMessage = "Price per seat is required.")]
		[Range(0, 10000, ErrorMessage = "Price cannot be a negative value.")]
		public decimal PricePerSeat { get; set; }

		[Range(0, 60, ErrorMessage = "Waiting time must be between 0 and 60 minutes.")]
		public int WaitingTimeMinutes { get; set; }

		[Required(ErrorMessage = "Ride preferences are required.")]
		public required CreateRidePreferencesRequestDto RidePreferences { get; set; }
	}

	public class CreateRidePreferencesRequestDto
	{
		public bool MusicAllowed { get; set; }
		public bool NoSmoking { get; set; }
		public bool PetsWelcomed { get; set; }
		public bool OpenToConversation { get; set; }
	}
}