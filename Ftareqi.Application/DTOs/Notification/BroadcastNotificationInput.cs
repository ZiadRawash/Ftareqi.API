using Ftareqi.Domain.Enums;
using Ftareqi.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Notification
{
	public sealed class BroadcastNotificationInput
	{
		public NotificationCategory Category { get; }

		public NotificationEventCode EventCode { get; }

		public string RelatedEntityId { get; }

		public NotificationMetadata MetaData { get; }

		public BroadcastNotificationInput(
			NotificationCategory category,
			NotificationEventCode eventCode,
			string relatedEntityId,
			NotificationMetadata metaData)
		{
			RelatedEntityId =
				relatedEntityId ??
				throw new ArgumentNullException(nameof(relatedEntityId));

			MetaData =
				metaData ??
				throw new ArgumentNullException(nameof(metaData));

			Category = category;
			EventCode = eventCode;
		}
	}
}
