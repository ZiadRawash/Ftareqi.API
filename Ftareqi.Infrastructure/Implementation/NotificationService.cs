using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Models;
using Ftareqi.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Ftareqi.Infrastructure.Implementation
{
	public class NotificationService : INotificationService
	{
		private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
		private readonly ILogger<NotificationService> _logger;

		public NotificationService(
			IHubContext<NotificationHub, INotificationClient> hubContext,
			ILogger<NotificationService> logger)
		{
			_hubContext = hubContext;
			_logger = logger;
		}

		public async Task NotifyAllAsync(NotificationDto notification)
		{
			try
			{
				await _hubContext.Clients.All.ReceiveNotification(notification);

				_logger.LogInformation("Notification broadcasted successfully.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Critical error occurred while broadcasting notification to all users.");
				throw; 
			}
		}

		public async Task NotifyUserAsync(string userId, NotificationDto notification)
		{
			try
			{
				await _hubContext.Clients.User(userId).ReceiveNotification(notification);
				_logger.LogInformation("Notification sent to SignalR pipeline for User: {UserId}", userId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send notification to user: {UserId}", userId);
				throw;
			}
		}

		public async Task NotifyGroupAsync(string groupName, NotificationDto notification)
		{
			try
			{
				await _hubContext.Clients.Group(groupName).ReceiveNotification(notification);

				_logger.LogInformation("Notification delivered to group: {GroupName} pipeline.", groupName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while sending notification to group: {GroupName}", groupName);
				throw;
			}
		}
	}
}