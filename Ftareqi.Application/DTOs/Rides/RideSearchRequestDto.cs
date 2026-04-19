using Ftareqi.Application.Common;
using Ftareqi.Application.QueryEnums;
using Ftareqi.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Ftareqi.Application.DTOs.Rides
{
	public class RideCriteria
	{
		[Range(-90d, 90d, ErrorMessage = "Start latitude must be between -90 and 90")]
		public double? StartLatitude { get; set; }

		[Range(-180d, 180d, ErrorMessage = "Start longitude must be between -180 and 180")]
		public double? StartLongitude { get; set; }

		[Range(-90d, 90d, ErrorMessage = "End latitude must be between -90 and 90")]
		public double? EndLatitude { get; set; }

		[Range(-180d, 180d, ErrorMessage = "End longitude must be between -180 and 180")]
		public double? EndLongitude { get; set; }

		[Range(1, 5, ErrorMessage = "Seats must be between 1 and 5")]
		public int? Seats { get; set; }

		public DateTime? DepartureTime { get; set; }

		public Gender? Gender { get; set; }
	}

	public class RideSearchRequestDto : GenericQueryReq
	{
		public RideCriteria? Criteria { get; set; }

		public RideField? Filters { get; set; }

	}
}
