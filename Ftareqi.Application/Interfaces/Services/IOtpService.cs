using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IOtpService
	{
		Task<Result<OTPDto>> GenerateOtpAsync(string userId, OTPPurpose purpose);
		Task<Result<int?>> VerifyOtpAsync(string userId, string code, OTPPurpose purpose);
	}

	//(background job)
	//Task CleanupExpiredOtpsAsync();

}
