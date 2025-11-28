using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Enums
{
	public enum OTPPurpose
	{
		PhoneVerification=0,
		PasswordReset=1,
		ChangePhone=2,
		TwoFactorLogin=3,
	}
}
