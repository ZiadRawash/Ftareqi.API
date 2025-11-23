using Ftareqi.Application.Common.Consts;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Orchestrators
{
	public class AuthOrchestrator : IAuthOrchestrator
	{
		private readonly IUserService _userService;
		private readonly ITokensService _tokensService;
		private readonly ILogger<AuthOrchestrator> _logger;
		public AuthOrchestrator(IUserService userService , ITokensService tokensService , ILogger<AuthOrchestrator> logger )
		{
			_userService = userService;
			_tokensService = tokensService;
			_logger = logger;
		}
		public async Task<Result> LoginAsync(LoginRequestDto request)
		{
			throw new NotImplementedException();
		}

		public async Task<Result> LogoutAsync(string userId)
		{
			throw new NotImplementedException();
		}

		public async Task<Result> RefreshTokenAsync(string refreshToken)
		{
			throw new NotImplementedException();
		}

		public async Task<Result> RegisterAsync(RegisterRequestDto request)
		{
		var userCreatinResult= await _userService.CreateUserAsync(request);
			if (userCreatinResult.IsFailure)
			{
				userCreatinResult.StatusCode = HttpStatusCodes.Unauthorized;
				return userCreatinResult;
			}
			_logger.LogInformation("Account has been created successfully for{UserId} ", userCreatinResult.Data?.Id);
			return Result.Success(message: "Account has been created successfully");
		//Otp validations
		}

	}
}
