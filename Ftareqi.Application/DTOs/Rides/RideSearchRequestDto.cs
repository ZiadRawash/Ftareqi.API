using Ftareqi.Application.Common;
using Ftareqi.Application.QueryEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ftareqi.Application.DTOs.Rides
{
	public class RideSearchRequestDto : GenericQueryReq
	{
		[Range(-90d, 90d, ErrorMessage = "Start latitude must be between -90 and 90")]

		public double StartLatitude { get; set; }

		[Range(-180d, 180d, ErrorMessage = "Start longitude must be between -180 and 180")]
		public double StartLongitude { get; set; }
		[Range(-90d, 90d, ErrorMessage = "End latitude must be between -90 and 90")]
		public double EndLatitude { get; set; }

		[Range(-180d, 180d, ErrorMessage = "End longitude must be between -180 and 180")]
		public double EndLongitude { get; set; }

		[Range(1, int.MaxValue, ErrorMessage = "Seats must be greater than zero")]
		public int Seats { get; set; }

		public RideField Filters { get; set; }

		public DateTime DepartureTime { get; set; }

	}
}
