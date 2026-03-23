using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Bookings;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Orchestrators
{
	public interface IRideOrchestrator
	{ 
		// create rideBookingRequest
		//1-validateMoney 2- validate the bookingParam against the ride 3- look money 3- create request 4-send notifications of looking money and the request for driver
		public Task<Result> CreateRideBookingRequest(CreateBookingRequestDto model, string userId);
	}
}
