using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Rides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IRideService
	{
		Task<Result> CreateRide(CreateRideRequestDto model, string userId);
		Task<Result<PaginatedResponse<DriverPastRidesResponse>>> GetDriverPastRides(GenericQueryReq request, string userId);
		Task<Result<PaginatedResponse<DriverUpcomingRidesResponse>>> GetDriverUpcomingRides(GenericQueryReq request, string userId);
	}
}
