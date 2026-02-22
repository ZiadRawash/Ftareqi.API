using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface INotificationBuilder
	{
		public Notification CreateNotificationAsync(string userId,
			NotificationCategory category,
			NotificationEventCode eventCode,
			string relatedEntityId,
			NotificationMetadata metaData);
	}
}
