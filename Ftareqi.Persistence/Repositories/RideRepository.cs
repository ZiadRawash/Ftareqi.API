using Ftareqi.Application.DTOs.Rides;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.QueryEnums;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Ftareqi.Persistence.Repositories
{
	public class RideRepository : BaseRepository<Ride>, IRideRepository
	{
		private readonly ApplicationDbContext _context;

		public RideRepository(ApplicationDbContext context) : base(context)
		{
			_context = context;
		}

		public async Task<(IReadOnlyList<RideSearchResponseDto> Items, int TotalCount)> SearchForRidesAsync(
	RideSearchRequestDto requestDto,
	string userId)
		{
			var requestDate = requestDto.DepartureTime.Date;
			var minDate = requestDate.AddDays(-2);
			var maxDate = requestDate.AddDays(3); 

			var startPoint = new Point(requestDto.StartLongitude, requestDto.StartLatitude) { SRID = 4326 };
			var endPoint = new Point(requestDto.EndLongitude, requestDto.EndLatitude) { SRID = 4326 };

			var maxStartDistanceInMeters = 10000;
			var maxEndDistanceInMeters = 10000;

			var baseQuery = _context.Rides
				.AsNoTracking()
				.Where(x =>
					x.Status == RideStatus.Scheduled &&
					x.DepartureTime >= minDate &&
					x.DepartureTime < maxDate &&
					x.AvailableSeats >= requestDto.Seats &&
					x.DriverProfile.UserId != userId &&
					x.StartLocation.Distance(startPoint) <= maxStartDistanceInMeters &&
					x.EndLocation.Distance(endPoint) <= maxEndDistanceInMeters
				);
			var totalCount = await baseQuery.CountAsync();
			var orderedQuery = baseQuery
				.OrderBy(x => Math.Abs(EF.Functions.DateDiffDay(x.DepartureTime, requestDate)));
			orderedQuery = requestDto.Filters switch
			{
				RideField.Cheapest => requestDto.SortDescending
					? orderedQuery.ThenByDescending(x => x.PricePerSeat)
					: orderedQuery.ThenBy(x => x.PricePerSeat),

				RideField.Nearby => requestDto.SortDescending
					? orderedQuery.ThenByDescending(x => x.StartLocation.Distance(startPoint) + x.EndLocation.Distance(endPoint))
					: orderedQuery.ThenBy(x => x.StartLocation.Distance(startPoint) + x.EndLocation.Distance(endPoint)),

				_ => orderedQuery.ThenBy(x => x.DepartureTime)
			};

			var items = await orderedQuery
				.Select(x => new RideSearchResponseDto
				{
					RideId = x.Id,
					DriverProfileId = x.DriverProfileId,
					StartLatitude = x.StartLocation.Y,
					StartLongitude = x.StartLocation.X,
					EndLatitude = x.EndLocation.Y,
					EndLongitude = x.EndLocation.X,
					DepartureTime = x.DepartureTime,
					AvailableSeats = x.AvailableSeats,
					PricePerSeat = x.PricePerSeat,
					Status = x.Status
				})
				.Skip((requestDto.Page - 1) * requestDto.PageSize)
				.Take(requestDto.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}