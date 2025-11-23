using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class LoginRequestDto
	{
		public required string Password { get; set; }
		public required string PhoneNumber { get; set; }
	}
}
