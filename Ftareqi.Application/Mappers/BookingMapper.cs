using Ftareqi.Application.DTOs.Bookings;
using Ftareqi.Domain.Models;

namespace Ftareqi.Application.Mappers
{
	public static class BookingMapper
	{
		public static DriverTripRequestResponseDto ToDriverTripRequestDto(this RideBooking booking)
		{
			return new DriverTripRequestResponseDto
			{
				BookingId = booking.Id,
				RideId = booking.RideId,
				RiderUserId = booking.UserId,
				RiderName = booking.User?.FullName ?? string.Empty,
				RequestedSeats = booking.NumOfSeats,
				TotalAmount = booking.NumOfSeats * (booking.Ride?.PricePerSeat ?? 0m),
				Status = booking.Status,
				RequestedAt = booking.BookedAt,
				DepartureTime = booking.Ride?.DepartureTime ?? booking.BookedAt,
				StartLatitude = booking.Ride?.StartLocation?.Y ?? 0,
				StartLongitude = booking.Ride?.StartLocation?.X ?? 0,
				EndLatitude = booking.Ride?.EndLocation?.Y ?? 0,
				EndLongitude = booking.Ride?.EndLocation?.X ?? 0
			};
		}

		public static UserTripRequestResponseDto ToUserTripRequestDto(this RideBooking booking)
		{
			return new UserTripRequestResponseDto
			{
				BookingId = booking.Id,
				RideId = booking.RideId,
				Status = booking.Status,
				BookedAt = booking.BookedAt,
				DepartureTime = booking.Ride?.DepartureTime ?? booking.BookedAt,
				Seats = booking.NumOfSeats,
				TotalAmount = booking.NumOfSeats * (booking.Ride?.PricePerSeat ?? 0m),
				DriverName = booking.Ride?.DriverProfile?.User?.FullName ?? string.Empty,
				DriverUserId = booking.Ride?.DriverProfile?.UserId ?? string.Empty,
				StartLatitude = booking.Ride?.StartLocation?.Y ?? 0,
				StartLongitude = booking.Ride?.StartLocation?.X ?? 0,
				EndLatitude = booking.Ride?.EndLocation?.Y ?? 0,
				EndLongitude = booking.Ride?.EndLocation?.X ?? 0
			};
		}
	}
}
