using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DripOut.Application.Common.Settings
{
	public class JWTSettings
	{
		public string? SignInKey { set; get; }
		public string? Audience { set; get; }
		public string? Issuer { set; get; }
		public string AccessTokenExpiryInMinutes { set; get; } = "10";

	}
}
