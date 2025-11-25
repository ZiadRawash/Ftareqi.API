using Ftareqi.Domain.Constants;
using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class OTP
	{
		public int Id { get; set; }
		public required string  CodeHash { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime ExpireAt { get; set; } = DateTime.UtcNow.AddMinutes(AuthConstants.OTPExpirationMinutes);
		public OTPPurpose Purpose { get; set; }
		public int FailedAttempts { get; set; }
		public bool IsUsed {  get; set; }
		public bool IsLocked => FailedAttempts >= AuthConstants.MaxOTPAttempts;
		public User? User { get; set; }
		[ForeignKey("User")]
		public required string UserId { get; set;}

	}
}
