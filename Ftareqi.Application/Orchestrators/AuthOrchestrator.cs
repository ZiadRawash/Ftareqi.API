using Ftareqi.Application.Common.Consts;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Constants;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.EntityFrameworkCore.Query.Internal;
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
		private readonly IOtpService _otpService;
		private readonly IRefreshTokenService _refreshTokenService;
		private readonly ILogger<AuthOrchestrator> _logger;
		private readonly IUserClaimsService _userClaimsService;
		public AuthOrchestrator(IUserService userService ,
			ITokensService tokensService ,
			ILogger<AuthOrchestrator> logger,
			IOtpService otpService,
			IRefreshTokenService refreshTokenService,
			IUserClaimsService userClaimsService)
		{
			_refreshTokenService = refreshTokenService;
			_otpService=otpService;
			_userService = userService;
			_tokensService = tokensService;
			_userClaimsService= userClaimsService;
			_logger = logger;
		}
		public async Task<Result<TokensDto>> LoginAsync(LoginRequestDto request)
		{
			if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Password))
			{
				return Result<TokensDto>.Failure("Phone number and password are required");
			}

			var credentialsValidationResult = await _userService.ValidateCredentialsAsync(request.PhoneNumber, request.Password);
			if (credentialsValidationResult.IsFailure)
			{
				return Result<TokensDto>.Failure(credentialsValidationResult.Message);
			}

			var userResult = await _userService.GetUserByPhoneAsync(request.PhoneNumber);
			if (userResult.IsFailure)
			{
				return Result<TokensDto>.Failure("Invalid phone number or password");
			}

			var user = userResult.Data;

			// Load user roles
			var rolesResult = await _userClaimsService.GetUserRolesAsync(user!.Id);
			if (rolesResult.IsFailure)
			{
				_logger.LogError("Failed to load roles for user {UserId}", user.Id);
				return Result<TokensDto>.Failure("An error occurred during login");
			}

			// Load user claims
			var claimsResult = await _userClaimsService.GetUserClaimsAsync(user.Id);
			if (claimsResult.IsFailure)
			{
				_logger.LogError("Failed to load claims for user {UserId}", user.Id);
				return Result<TokensDto>.Failure("An error occurred during login");
			}

			// Create DTO for access token generation
			var createAccessTokenDto = new CreateAccessTokenDto
			{
				UserId = user.Id,
				Roles = rolesResult.Data!,
				AdditionalClaims = claimsResult.Data!
			};

			// Generate access token
			var accessTokenResult = _tokensService.GenerateAccessToken(createAccessTokenDto);
			if (accessTokenResult.IsFailure)
			{
				_logger.LogError("Failed to generate access token for user {UserId}", user.Id);
				return Result<TokensDto>.Failure("An error occurred during login");
			}

			// Generate refresh token
			var refreshTokenResult = _tokensService.GenerateRandomToken();
			if (refreshTokenResult.IsFailure)
			{
				_logger.LogError("Failed to generate refresh token for user {UserId}", user.Id);
				return Result<TokensDto>.Failure("An error occurred during login");
			}

			// Store refresh token
			var tokenCreated = await _refreshTokenService.CreateAsync(user.Id, refreshTokenResult.Data!);
			if (tokenCreated.IsFailure)
			{
				_logger.LogError("Failed to store refresh token for user {UserId}", user.Id);
				return Result<TokensDto>.Failure("Error happened while creating refresh token");
			}

			_logger.LogInformation("User {UserId} logged in successfully with {RoleCount} roles and {ClaimCount} claims",
				user.Id, rolesResult.Data!.Count(), claimsResult.Data!.Count);

			return Result<TokensDto>.Success(new TokensDto
			{
				AccessToken = accessTokenResult.Data,
				RefreshToken = tokenCreated.Data!.Token
			}, "Login successful");
		}
		public async Task<Result> LogoutAsync(string refreshToken)
		{
			var tokenfound = await _refreshTokenService.InvalidateAsync(refreshToken);
			if (tokenfound.IsFailure)
				return Result.Failure(tokenfound.Errors);
			return Result<TokensDto>.Success("Refresh token has been revoked successfully");
		}

		public async Task<Result<string>> RefreshTokenAsync(string refreshToken)
		{
			var usedId= await _refreshTokenService.GetUserFromRefreshTokenAsync(refreshToken);
			if (usedId.IsFailure) { 
				return Result<string>.Failure(usedId.Errors);
			}
			// Load user roles
			var rolesResult = await _userClaimsService.GetUserRolesAsync(usedId.Data!);
			if (rolesResult.IsFailure)
			{
				_logger.LogError("Failed to load roles for user {UserId}", usedId.Data!);
				return Result<string>.Failure("An error occurred during creating access token");
			}

			// Load user claims
			var claimsResult = await _userClaimsService.GetUserClaimsAsync(usedId.Data!);
			if (claimsResult.IsFailure)
			{
				_logger.LogError("Failed to load claims for user {UserId}", usedId.Data!);
				return Result<string>.Failure("An error occurred during creating access token");
			}
			var createAccessTokenDto = new CreateAccessTokenDto
			{
				UserId = usedId.Data!,
				Roles = rolesResult.Data!,
				AdditionalClaims = claimsResult.Data!
			};
			var accessToken=  _tokensService.GenerateAccessToken(createAccessTokenDto);
			if (accessToken.IsFailure) {
				return Result<string>.Failure("An error occurred during creating access token");
			}
			return Result<string>.Success(data: accessToken.Data!);
		}
		public async Task<Result> RegisterAsync(RegisterRequestDto request)
		{

			var userCreationResult = await _userService.CreateUserAsync(request);
			if (userCreationResult.IsFailure)
			{
				return userCreationResult;
			}

			var userId = userCreationResult.Data?.Id;
			_logger.LogInformation("Account has been created successfully for {UserId}", userId);

			var roleAdded= await _userClaimsService.AddRolesAsync(userId!,new[] { Roles.User });
			if (roleAdded.IsSuccess)
			{
				_logger.LogInformation("role _User_ has been added successfully to {UserId}", userId);
			}
		
			// 2. Generate and send OTP for phone verification
			var otpResult = await _otpService.GenerateOtpAsync(userId!, OTPPurpose.PhoneVerification);
			if (otpResult.IsFailure)
			{
				// Optional: log the failure but still return success for registration
				_logger.LogWarning("Failed to send OTP for user {UserId}: {Error}", userId, otpResult.Errors.ToString());
				// You may also choose to fail registration, depending on business rules
				return Result.Failure("Account created, but failed to send verification OTP");
			}

			// 3. Return success
			return Result.Success("Account created successfully. Verification OTP has been sent.");
		}
		public async Task<Result> ValidateOtpAsync(string phoneNumber, string code, OTPPurpose purpose)
		{
			var userFound = await _userService.GetUserByPhoneAsync(phoneNumber);
			if (userFound.IsFailure)
				return Result.Failure(userFound.Errors);

			var validated = await _otpService.VerifyOtpAsync(userFound.Data!.Id, code, purpose);
			if (!validated.IsSuccess)
				return Result.Failure( validated.Errors,message:validated.Data.ToString());

			var isValidated = await _userService.ConfirmPhoneNumber(userFound.Data!.Id);
			if (!isValidated.IsSuccess)
				return Result.Failure(isValidated.Errors);

			return Result.Success("Phone number confirmed successfully.");
		}

	}
}
