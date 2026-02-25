using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.Interfaces.Services;
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
		private readonly IFcmService _fcmService;
		private readonly IFcmTokenService _fcmTokenService;
		public NotificationService(
			IHubContext<NotificationHub, INotificationClient> hubContext,
			ILogger<NotificationService> logger,
			IFcmService fcmService,
			IFcmTokenService fcmTokenService)
		{
			_hubContext = hubContext;
			_logger = logger;
			_fcmService = fcmService;
			_fcmTokenService = fcmTokenService;
		}

		public async Task NotifyAllAsync(NotificationDto notification)
		{
			try
			{
				// SignalR Broadcast
				try
				{
					await _hubContext.Clients.All.ReceiveNotification(notification);
					_logger.LogInformation("SignalR broadcast sent successfully.");
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex,
						"SignalR broadcast failed");
				}
				//  Get All Tokens
				var tokens =
					await _fcmTokenService
					.GetAllActiveTokensAsync();
				if (!tokens.Any())
				{
					_logger.LogWarning(
						"No active FCM tokens found");
					return;
				}
				//  Prepare Data
				var fcmData =
					new Dictionary<string, string>
					{
						{ "id", notification.Id.ToString() },
						{ "title", notification.Title },
						{ "category",notification.Category.ToString() },
						{ "eventCode",notification.EventCode.ToString() },
						{ "relatedEntityId",notification.RelatedEntityId ?? "" },
						{ "isRead",notification.IsRead.ToString() },
						{ "createdAt",notification.CreatedAt.ToString("O") },
						{ "data", Newtonsoft.Json.JsonConvert.SerializeObject(notification.Data)}
					};
				// Send FCM
				var result =
					await _fcmService
					.SendMultipleNotificationsAsync(
						tokens,
						notification.Title,
						"You have a new notification",
						fcmData
					);

				// Cleanup invalid tokens

				if (result.InvalidTokens.Any())
				{
					foreach (var token in result.InvalidTokens)
					{
						await _fcmTokenService.MarkTokenInvalidAsync(token);
					}

					_logger.LogInformation("Removed {Count} invalid tokens",result.InvalidTokens.Count);
				}
				// Final Log
				_logger.LogInformation(
					"NotifyAll → Success:{S} Invalid:{I} Failed:{F}",
					result.SuccessTokens.Count,
					result.InvalidTokens.Count,
					result.FailedTokens.Count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,"Critical error in NotifyAllAsync");
				throw;
			}
		}
		public async Task NotifyUserAsync(
			string userId,
			NotificationDto notificationDto)
		{
			try
			{
				//SignalR
				try
				{
					await _hubContext
						.Clients
						.User(userId)
						.ReceiveNotification(notificationDto);
					_logger.LogInformation("SignalR notification sent → User {UserId}",userId);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex,"SignalR failed → User {UserId}",userId);
				}
				// FCM
				var tokens =
					await _fcmTokenService
					.GetActiveTokensAsync(userId);
				if (!tokens.Any())
				{
					_logger.LogWarning("No FCM tokens for user {UserId}",userId);
					return;
				}
				var fcmData =
					new Dictionary<string, string>
					{
						{ "id", notificationDto.Id.ToString() },
						{ "title", notificationDto.Title },
						{ "category", notificationDto.Category.ToString() },
						{ "eventCode", notificationDto.EventCode.ToString() },
						{ "relatedEntityId",
							notificationDto.RelatedEntityId ?? "" },
						{ "isRead",notificationDto.IsRead.ToString() },
						{ "createdAt",notificationDto.CreatedAt.ToString("O") },
						{ "data", JsonConvert.SerializeObject( notificationDto.Data)}
					};
				var previewText = "You have a new notification";
				// Send FCM
				var result =
					await _fcmService
					.SendMultipleNotificationsAsync(
						tokens,
						notificationDto.Title,
						previewText,
						fcmData
					);
				// Remove invalid tokens
				if (result.InvalidTokens.Any())
				{
					foreach (var token in result.InvalidTokens)
					{
						await _fcmTokenService.MarkTokenInvalidAsync(token);
					}
					_logger.LogInformation("Invalid tokens removed: {Count}",result.InvalidTokens.Count);
				}
				_logger.LogInformation(
					"FCM → Success:{S} Invalid:{I} Failed:{F}",
					result.SuccessTokens.Count,
					result.InvalidTokens.Count,
					result.FailedTokens.Count
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,"Error in NotifyUserAsync");
				throw;
			}
		}
	}
}