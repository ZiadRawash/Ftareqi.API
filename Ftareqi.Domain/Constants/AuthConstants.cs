using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Constants
{
	public static class AuthConstants
	{
		public const int RefreshTokenExpirationDays = 7;
		public const int RefreshTokenSize = 32;
		public const double OTPExpirationMinutes=13.00;
		public const int MaxOTPAttempts = 4;
		public const int MaxNumOfActiveRefreshTokens = 5;

	}
}
