using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class FcmToken
	{
		public int Id { get; set; }
		public string Token { get; set; } = default!;
		public string UserId { get; set; } = default!;
		public User User { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? LastUsedAt { get; set; }
		public bool IsActive { get; set; } = true;
	}
}
