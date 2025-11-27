using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class RefreshToken
	{
		[Required(ErrorMessage = "Token is required.")]
		[StringLength(200, MinimumLength = 10, ErrorMessage = "Token must be between 10 and 200 characters.")]
		public required string Token { get; set; }
	}
}
