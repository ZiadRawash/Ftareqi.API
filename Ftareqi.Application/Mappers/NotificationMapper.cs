using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Domain.Models;

namespace Ftareqi.Application.Mappers
{
	public static class NotificationMapper
	{
		public static NotificationDto ToDto(Notification notification)
		{
			if (notification == null)
				throw new ArgumentNullException(nameof(notification));

			return new NotificationDto
			{
				Id = notification.Id,
				Title = notification.Title,
				Category = notification.Category,
				EventCode = notification.EventCode,
				RelatedEntityId = notification.RelatedEntityId,
				Data = NotificationDataHelper.DeserializeData(notification.Data),
				IsRead = notification.IsRead,
				CreatedAt = notification.CreatedAt
			};
		}
	}
}