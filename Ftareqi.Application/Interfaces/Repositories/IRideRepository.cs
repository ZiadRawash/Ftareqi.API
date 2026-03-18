using Ftareqi.Application.DTOs.Rides;
using Ftareqi.Domain.Models;

namespace Ftareqi.Application.Interfaces.Repositories
{
	public interface IRideRepository : IBaseRepository<Ride>
	{
		Task<(IReadOnlyList<RideSearchResponseDto> Items, int TotalCount)> SearchForRidesAsync(
			RideSearchRequestDto requestDto,
			string userId
			);
	}
}