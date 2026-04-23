using Ftareqi.Application.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface ISmsService
	{
		public Task<Result> SendSMS(string phoneNumber , string otp);
	}
}
