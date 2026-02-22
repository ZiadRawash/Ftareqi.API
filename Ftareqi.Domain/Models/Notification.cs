using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class Notification
	{
		public int Id { get; set; }
		public string Title { get; set; } = default!;

		public NotificationCategory Category { get; set; }

		public NotificationEventCode EventCode { get; set; }

		public string? RelatedEntityId { get; set; } 

		public string? Data { get; set; } // JSON

		public bool IsRead { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public string UserId { get; set; } = default!;
		public User User { get; set; }
	}
}
