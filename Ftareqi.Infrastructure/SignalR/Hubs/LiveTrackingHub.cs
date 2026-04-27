using Ftareqi.Application.Common.Consts;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.LiveTracking;
using Ftareqi.Application.Interfaces.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Twilio.Jwt.Taskrouter;

namespace Ftareqi.Infrastructure.SignalR.Hubs
{
	[Authorize]
	public class LiveTrackingHub : Hub<IliveTrackingClient>
	{
		private readonly ILogger<LiveTrackingHub> _logger;

		public LiveTrackingHub(ILogger<LiveTrackingHub> logger)
		{
			_logger = logger;
		}
		[Authorize]
		public override async Task OnConnectedAsync()
		{
			var httpContext = Context.GetHttpContext();
			var rideId = httpContext.Request.Query["rideId"];

			if (!string.IsNullOrEmpty(rideId))
			{
				await Groups.AddToGroupAsync(Context.ConnectionId, $"trip_{rideId}");
				_logger.LogInformation("Connection {Id} joined Trip Group: {RideId}", Context.ConnectionId, rideId);
			}
			else
			{
				_logger.LogWarning("Connection {Id} attempted to connect without a rideId.", Context.ConnectionId);
			}

			await base.OnConnectedAsync();
		}

		[Authorize(Policy = "DriverOnly")]
		public async Task SendLocation(DriverCoordinatesDto model)
		{
			var rideId = Context.GetHttpContext()?.Request.Query["rideId"];
			var result = new DriveLocationResultDto
			{
				IsSuccess = true,
				Latitude = model.Latitude,
				Longitude = model.Longitude,
			};
			if (!string.IsNullOrEmpty(rideId))
			{
				await Clients.OthersInGroup($"trip_{rideId}").ReceiveDriverCoordinates(result);
			}
		}

		[Authorize]
		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			var isDriverClaim = Context.User?.FindFirst(CustomClaimTypes.IsDriver)?.Value;
			var rideId = Context.GetHttpContext()?.Request.Query["rideId"];

			if (!string.IsNullOrEmpty(rideId) && isDriverClaim == CustomClaimTypes.True)
			{
				var result = new DriveLocationResultDto
				{
					IsSuccess = false, 
					Message = "Driver Connection Lost"
				};

				_logger.LogWarning("Driver for Trip {RideId} disconnected!", rideId!);
				await Clients.OthersInGroup($"trip_{rideId}").ReceiveDriverCoordinates(result);
			}

			await base.OnDisconnectedAsync(exception);
		}
	}
}
