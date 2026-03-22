using Ftareqi.Application.Common;
using Ftareqi.Application.QueryEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Bookings
{
	public class GetTripsRequestsDto : GenericQueryReq
	{
		public BookingStatusQueryEnum? FilterBy { get; set; }
	}

}
