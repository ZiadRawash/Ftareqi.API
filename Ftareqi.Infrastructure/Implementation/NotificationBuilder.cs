using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Ftareqi.Infrastructure.Implementation
{
	public class NotificationBuilder : INotificationBuilder
	{

		public  Notification CreateNotificationAsync(string userId, NotificationCategory category, NotificationEventCode eventCode, string relatedEntityId, NotificationMetadata metaData)
		{
			var notification = new Notification
			{
				UserId = userId,
				Category = category,
				CreatedAt = DateTime.UtcNow,
				Title = GetTitleForEventCode(eventCode),
				IsRead = false,
				RelatedEntityId = relatedEntityId,
				EventCode = eventCode,
				Data = JsonSerializer.Serialize(metaData)

			};
			return notification;
		}
		private string GetTitleForEventCode(NotificationEventCode eventCode) => eventCode switch
		{
			NotificationEventCode.WalletCharged => "Wallet charged",
			NotificationEventCode.WalletWithdrawn => "wallet withdrawn",
			_ => "Notification"
		};
	}
}
