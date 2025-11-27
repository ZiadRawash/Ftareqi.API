using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class PhoneWithResetOtpDto
	{
		[Required(ErrorMessage = "OTP is required")]
		[StringLength(6, MinimumLength = 4, ErrorMessage = "OTP must be between 4 and 6 characters")]
		[RegularExpression(@"^\d+$", ErrorMessage = "OTP must contain only numbers")]
		public required string otp { get; set; }

		[Required(ErrorMessage = "Phone number is required")]
		[Phone(ErrorMessage = "Invalid phone number format")]
		public required string PhoneNumber { get; set; }
	}
}
