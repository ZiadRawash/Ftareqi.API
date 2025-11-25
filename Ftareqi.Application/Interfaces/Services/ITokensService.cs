using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
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
		Result<string> GenerateAccessToken(CreateAccessTokenDto data);
		Result<string> GenerateRandomToken();
		Result<ClaimsPrincipal?> ValidateAccessToken(string token);

	}
}
