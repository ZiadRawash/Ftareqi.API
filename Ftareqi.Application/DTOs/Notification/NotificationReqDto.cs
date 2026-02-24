using Ftareqi.Domain.Enums;
using Ftareqi.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Notification
{
	public class NotificationDto
	{
		public int Id { get; set; }
		public string Title { get; set; } = default!;

		public NotificationCategory Category { get; set; }

		public NotificationEventCode EventCode { get; set; }

		public string? RelatedEntityId { get; set; }

		public object  Data { get; set; } 

		public bool IsRead { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	}
}
