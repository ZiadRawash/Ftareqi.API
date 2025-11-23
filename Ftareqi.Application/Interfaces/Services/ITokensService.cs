using Ftareqi.Application.Common.Results;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface ITokensService
	{
		 Result<string> GenerateAccessToken(Guid userId, IEnumerable<string>roles , Dictionary<string,string> additionalClaims );
		Result<RefreshToken> GenerateRefreshToken( string userId);
		Result<ClaimsPrincipal?> ValidateAccessToken(string token);

	}
}
