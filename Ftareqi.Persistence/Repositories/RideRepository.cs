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
			var now = DateTime.UtcNow;
			var criteria = requestDto.Criteria;
			var requestTime = criteria?.DepartureTime ?? now;

			var maxStartDistanceInMeters = 10000;
			var maxEndDistanceInMeters = 10000;

			var baseQuery = _context.Rides
				.AsNoTracking()
				.Where(x =>
					x.Status == RideStatus.Scheduled &&
					x.DepartureTime >= now &&
					x.AvailableSeats > 0 &&
					x.DriverProfile.UserId != userId
				);

			if (criteria != null)
			{
				var minDate = criteria.DepartureTime!.Value.AddDays(-2);
				var maxDate = criteria.DepartureTime!.Value.AddDays(3);
				var startPoint = new Point(criteria.StartLongitude!.Value, criteria.StartLatitude!.Value) { SRID = 4326 };
				var endPoint = new Point(criteria.EndLongitude!.Value, criteria.EndLatitude!.Value) { SRID = 4326 };

				baseQuery = baseQuery.Where(x =>
					x.DepartureTime >= minDate &&
					x.DepartureTime < maxDate &&
					x.AvailableSeats >= criteria.Seats!.Value &&
					x.StartLocation.Distance(startPoint) <= maxStartDistanceInMeters &&
					x.EndLocation.Distance(endPoint) <= maxEndDistanceInMeters);
			}

			if (criteria?.Gender != null)
			{
				baseQuery = baseQuery.Where(x => x.DriverProfile.User!.Gender == criteria.Gender);
			}
			var totalCount = await baseQuery.CountAsync();

			IOrderedQueryable<Ride> orderedQuery;
			if (criteria != null)
			{
				var startPoint = new Point(criteria.StartLongitude!.Value, criteria.StartLatitude!.Value) { SRID = 4326 };
				var endPoint = new Point(criteria.EndLongitude!.Value, criteria.EndLatitude!.Value) { SRID = 4326 };
				orderedQuery = baseQuery
					.OrderBy(x => Math.Abs(EF.Functions.DateDiffHour(x.DepartureTime, requestTime)))
					.ThenBy(x => x.StartLocation.Distance(startPoint) + x.EndLocation.Distance(endPoint));
				orderedQuery = ApplySorting(orderedQuery, requestDto, isSecondary: true);
			}
			else
			{
				orderedQuery = ApplySorting(baseQuery, requestDto, isSecondary: false);
			}

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
					Status = x.Status,
					DriverRate = x.DriverProfile.RatingCount == 0 ? null : Math.Round(((double)x.DriverProfile.RatingSum / x.DriverProfile.RatingCount) * 2) / 2.0,
					DriverImgUrl = x.DriverProfile.Images.Where(img => img.Type == ImageType.DriverProfilePhoto).Select(img => img.Url).FirstOrDefault() ?? null!,
					DriverName = x.DriverProfile.User!.FullName,
					EndAddress = x.EndAddress,
					StartAddress= x.StartAddress,
				})
				.Skip((requestDto.Page - 1) * requestDto.PageSize)
				.Take(requestDto.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}

		private static IOrderedQueryable<Ride> ApplySorting(IQueryable<Ride> query, RideSearchRequestDto requestDto, bool isSecondary)
		{
			if (isSecondary && query is IOrderedQueryable<Ride> orderedQuery)
			{
				return requestDto.Filters switch
				{
					RideField.Cheapest => requestDto.SortDescending
						? orderedQuery.ThenByDescending(x => x.PricePerSeat)
						: orderedQuery.ThenBy(x => x.PricePerSeat),
					RideField.HighestRate => requestDto.SortDescending
						? orderedQuery.ThenByDescending(x => x.DriverProfile.RatingCount > 0
							? (double)x.DriverProfile.RatingSum / x.DriverProfile.RatingCount
							: 0)
						: orderedQuery.ThenBy(x => x.DriverProfile.RatingCount > 0
							? (double)x.DriverProfile.RatingSum / x.DriverProfile.RatingCount
							: 0),
					_ => requestDto.SortDescending
						? orderedQuery.ThenByDescending(x => x.DepartureTime)
						: orderedQuery.ThenBy(x => x.DepartureTime)
				};
			}

			return requestDto.Filters switch
			{
				RideField.Cheapest => requestDto.SortDescending
					? query.OrderByDescending(x => x.PricePerSeat)
					: query.OrderBy(x => x.PricePerSeat),
				RideField.HighestRate => requestDto.SortDescending
					? query.OrderByDescending(x => x.DriverProfile.RatingCount > 0
						? (double)x.DriverProfile.RatingSum / x.DriverProfile.RatingCount
						: 0)
					: query.OrderBy(x => x.DriverProfile.RatingCount > 0
						? (double)x.DriverProfile.RatingSum / x.DriverProfile.RatingCount
						: 0),
				_ => requestDto.SortDescending
					? query.OrderByDescending(x => x.DepartureTime)
					: query.OrderBy(x => x.DepartureTime)
			};
		}
	}
}