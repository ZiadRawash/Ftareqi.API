using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Rides
{
	public class CreateRideRequestDto
	{
		public double StartLatitude { get; set; }
		public double StartLongitude { get; set; }
		public string StartAddress { get; set; } = string.Empty;
		public double EndLatitude { get; set; }
		public double EndLongitude { get; set; }
		public string EndAddress { get; set; } = string.Empty;
		public DateTime DepartureTime { get; set; }
		public int TotalSeats { get; set; }
		public decimal PricePerSeat { get; set; }
		public int WaitingTimeMinutes { get; set; }
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
