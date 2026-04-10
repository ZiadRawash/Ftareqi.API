using Ftareqi.Application.Common;
using Ftareqi.Application.QueryEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ftareqi.Application.DTOs.Rides
{
	public class RideSearchRequestDto : GenericQueryReq
	{
		[Required]
		[Range(-90d, 90d, ErrorMessage = "Start latitude must be between -90 and 90")]
		public double StartLatitude { get; set; }
		[Required]
		[Range(-180d, 180d, ErrorMessage = "Start longitude must be between -180 and 180")]
		public double StartLongitude { get; set; }
		[Required]
		[Range(-90d, 90d, ErrorMessage = "End latitude must be between -90 and 90")]
		public double EndLatitude { get; set; }
		[Required]
		[Range(-180d, 180d, ErrorMessage = "End longitude must be between -180 and 180")]
		public double EndLongitude { get; set; }
		[Required]
		[Range(1, 5, ErrorMessage = "Seats must be greater than zero")]
		public int Seats { get; set; }
		[Required]
		public DateTime DepartureTime { get; set; }
		public RideField Filters { get; set; }

	}
}
