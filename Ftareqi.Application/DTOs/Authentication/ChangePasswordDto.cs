using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class ChangePasswordDto
	{
		public string UserId { get; set; } = default!;
		public string OldPassword { get; set; } = default!;
		public string NewPassword { get; set; } = default!;
	}
}
