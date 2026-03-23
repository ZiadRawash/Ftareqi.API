using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs;
using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Ftareqi.Infrastructure.Implementation
{
	public class NotificationBuilder : INotificationBuilder
	{
		private readonly ILogger<NotificationBuilder> _logger;

		public NotificationBuilder(ILogger<NotificationBuilder> logger)
		{
			_logger = logger;
		}

		public Notification CreateNotification(NotificationInput builderDto)
		{
			try
			{
				_logger.LogInformation("Building notification for User: {UserId}, EventCode: {EventCode}",
					builderDto.UserId, builderDto.EventCode);
				var title = GetTitleForEventCode(builderDto.EventCode);
				var notification = new Notification
				{
					UserId = builderDto.UserId,
					Category = builderDto.Category,
					CreatedAt = DateTime.UtcNow,
					Title = title,
					IsRead = false,
					RelatedEntityId = builderDto.RelatedEntityId,
					EventCode = builderDto.EventCode,
					Data = NotificationDataHelper.SerializeData(builderDto.MetaData)
				};

				_logger.LogInformation("Notification object created successfully with Title: '{Title}'", title);
				return notification;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to build notification for User: {UserId}. Error: {Message}",
					builderDto.UserId, ex.Message);
				throw;
			}
		}
		public Notification CreateBroadcastNotification(
			BroadcastNotificationInput builderDto)
		{
			try
			{
				_logger.LogInformation(
					"Building BROADCAST notification. EventCode: {EventCode}",
					builderDto.EventCode);

				var title =
					GetTitleForEventCode(
						builderDto.EventCode);

				var notification =
					new Notification
					{
						UserId = null!,

						Category =builderDto.Category,

						CreatedAt =DateTime.UtcNow,

						Title =title,

						IsRead =false,

						RelatedEntityId =builderDto.RelatedEntityId,

						EventCode =builderDto.EventCode,

						Data =NotificationDataHelper.SerializeData(builderDto.MetaData),
						IsBroadcast = true
					};
				_logger.LogInformation(
					"Broadcast notification created successfully");
				return notification;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,
					"Failed building broadcast notification");
				throw;
			}
		}
		private string GetTitleForEventCode(NotificationEventCode eventCode)
		{
			var title = eventCode switch
			{
				NotificationEventCode.WalletCharged => "Wallet charged",
				NotificationEventCode.WalletWithdrawn => "Wallet withdrawn",
				NotificationEventCode.AmountReserved => "Amount reserved",
				NotificationEventCode.AmountReleased => "Amount released",
				NotificationEventCode.Approved => "Driver Registration Approved",
				NotificationEventCode.Rejected => "Driver Registration Rejected",
				NotificationEventCode.Expired => "Driver Account Expired",
				NotificationEventCode.bookingAccepted => "Request Accepted ",
				NotificationEventCode.bookingDeclined => "Request Declined",
				NotificationEventCode.bookingRequest => "New Ride Request",
				_ => "Notification"
			};

			if (title == "Notification")
			{
				_logger.LogWarning("EventCode {EventCode} hit the default title case.", eventCode);
			}

			return title;
		}
	}
}