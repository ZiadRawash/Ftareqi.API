using Ftareqi.Domain.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class RefreshToken
	{
		public int Id { get; set; }
		public required string Token { get; set; }
		public DateTime CreatedOn { get; private set; } = DateTime.UtcNow;
		public DateTime? RevokedOn { get; set; }
		public DateTime ExpiresOn { get; private set; } = DateTime.UtcNow.AddDays(AuthConstants.RefreshTokenExpirationDays);
		public bool IsActive => !RevokedOn.HasValue && DateTime.UtcNow <= ExpiresOn;

		public User? User { get; set; }
		[ForeignKey("User")]
		public required string UserId { get; set; }
	}
}

