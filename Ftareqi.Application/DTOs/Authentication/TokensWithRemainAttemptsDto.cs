using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class TokensWithRemainAttempts
	{
		public	int? RemainingAttempts { get; set; }
		public string? AccessToken { get; set; }
		public string? RefreshToken { get; set; }

	}
}
