using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Bookings;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Ftareqi.Domain.ValueObjects;
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
		public Task<Result> CreateRideBookingRequest(CreateBookingRequestDto model, string userId);
		public Task<Result> AcceptRideBookingRequest(int bookingId, string driverId);
		public Task<Result> DeclineRideBookingRequest(int bookingId, string driverId);
		public Task<Result> CancelRideBookingByRider(int bookingId, string riderId);
	}

	}
