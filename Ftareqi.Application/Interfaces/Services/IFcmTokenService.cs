using Ftareqi.Application.Common.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IFcmTokenService
	{
		Task<Result> RegisterDeviceAsync(string userId, string token);

		Task<Result> DeactivateDeviceAsync(string userId, string token);

		Task<List<string>> GetActiveTokensAsync(string userId);
		Task<List<string>> GetAllActiveTokensAsync();

		Task<Result> MarkTokenInvalidAsync(string token);

	}
}
