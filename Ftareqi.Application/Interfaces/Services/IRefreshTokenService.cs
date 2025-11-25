using Ftareqi.Application.Common.Results;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IRefreshTokenService
	{
		Task<Result<string>> GetUserFromRefreshTokenAsync(string refreshToken);
		Task<Result<RefreshToken>> CreateAsync(string userId, string refreshTokenString);
		Task<Result<RefreshToken?>> FindValidRefreshTokenAsync(string userId, string token);
		Task<Result> InvalidateAsync(string refreshToken);
		Task<Result> InvalidateAllForUserAsync(string userId);
		Task<Result<RefreshToken>> RotateAsync(RefreshToken oldToken, string newRefreshTokenString);
	}
}
