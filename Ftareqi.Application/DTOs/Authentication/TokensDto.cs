using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class TokensDto
	{ 
		public string? AccessToken {  get; set; }
		public string? RefreshToken { get; set; }
	}
}
