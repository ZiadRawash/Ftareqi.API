using Ftareqi.Application.Common;
using Ftareqi.Application.QueryEnums;

namespace Ftareqi.Application.DTOs.Bookings
{
	public class GetUpcomingTripsRequestsDto : GenericQueryReq
	{
		public UpcomingTripStatusQueryEnum? FilterBy { get; set; }
	}
}
