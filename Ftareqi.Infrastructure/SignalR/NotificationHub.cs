using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.SignalR
{
	

	[Authorize] 
	public class NotificationHub : Hub<INotificationClient>
	{
		private readonly ILogger<NotificationHub> _logger;

		public NotificationHub(ILogger<NotificationHub> logger)
		{
			_logger = logger;
		}

		public override async Task OnConnectedAsync()
		{
			var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
						 ?? Context.User?.FindFirst("sub")?.Value
						 ?? Context.User?.FindFirst("nameid")?.Value;

			if (!string.IsNullOrEmpty(userId))
			{
				await Groups.AddToGroupAsync(Context.ConnectionId, userId);
				_logger.LogInformation("User {UserId} connected and added to group.", userId);
			}
			else
			{
				_logger.LogWarning("Connection established but no User ID found in claims.");
			}

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			try
			{
				var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				_logger.LogInformation($"User {userId} disconnected from NotificationHub");

				await base.OnDisconnectedAsync(exception);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in OnDisconnectedAsync: {ex.Message}");
				throw ;
			}
		}
	}
}
