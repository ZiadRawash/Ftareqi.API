using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class LoginRequestDto
	{
		[Required(ErrorMessage = "Password is required.")]
		[MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
		public required string Password { get; set; }

		[Required(ErrorMessage = "Password is required.")]
		public required string PhoneNumber { get; set; }
	}
}
