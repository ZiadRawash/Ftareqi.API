using Ftareqi.Application.DTOs.Rides;
using Ftareqi.Domain.Models;
using NetTopologySuite.Geometries;

namespace Ftareqi.Application.Mappers
{
	public static class RideMapper
	{
		public static Ride ToEntity(this CreateRideRequestDto dto, int driverProfileId)
		{
			var now = DateTime.UtcNow;

			return new Ride
			{
				StartLocation = new Point(dto.StartLongitude, dto.StartLatitude) { SRID = 4326 },
				StartAddress = dto.StartAddress,
				EndLocation = new Point(dto.EndLongitude, dto.EndLatitude) { SRID = 4326 },
				EndAddress = dto.EndAddress,
				DepartureTime = dto.DepartureTime,
				TotalSeats = dto.TotalSeats,
				AvailableSeats = dto.TotalSeats,
				PricePerSeat = dto.PricePerSeat,
				WaitingTime = TimeSpan.FromMinutes(dto.WaitingTimeMinutes),
				CreatedAt = now,
				UpdatedAt = now,
				DriverProfileId = driverProfileId,
				RidePreferences = dto.RidePreferences.ToEntity()
			};
		}

		public static RidePreferences ToEntity(this CreateRidePreferencesRequestDto dto)
		{
			return new RidePreferences
			{
				MusicAllowed = dto.MusicAllowed,
				NoSmoking = dto.NoSmoking,
				PetsWelcomed = dto.PetsWelcomed,
				OpenToConversation = dto.OpenToConversation
			};
		}
	}
}