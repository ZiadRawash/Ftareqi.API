using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Ftareqi.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Notification
{
	public sealed class NotificationInput
	{
		public string UserId { get; }
		public NotificationCategory Category { get; }
		public NotificationEventCode EventCode { get; }
		public string RelatedEntityId { get; }
		public NotificationMetadata MetaData { get; }

		public NotificationInput(
			string userId,
			NotificationCategory category,
			NotificationEventCode eventCode,
			string relatedEntityId,
			NotificationMetadata metaData)
		{
			UserId = userId ?? throw new ArgumentNullException(nameof(userId));
			RelatedEntityId = relatedEntityId ?? throw new ArgumentNullException(nameof(relatedEntityId));
			MetaData = metaData ?? throw new ArgumentNullException(nameof(metaData));
			Category = category;
			EventCode = eventCode;
		}
	}
}
