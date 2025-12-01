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
using System.ComponentModel.DataAnnotations;
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
			return Result.Success("Refresh token has been revoked successfully");
		}
		public async Task<Result<AccessTokenDto>> RefreshAccessToken(string refreshToken)
		{
			var usedId= await _refreshTokenService.GetUserFromRefreshTokenAsync(refreshToken);
			if (usedId.IsFailure) { 
				return Result<AccessTokenDto>.Failure(usedId.Errors);
			}
			var rolesResult = await _userClaimsService.GetUserRolesAsync(usedId.Data!);
			if (rolesResult.IsFailure)
			{
				_logger.LogError("Failed to load roles for user {UserId}", usedId.Data!);
				return Result<AccessTokenDto>.Failure("An error occurred during creating access token");
			}

			var claimsResult = await _userClaimsService.GetUserClaimsAsync(usedId.Data!);
			if (claimsResult.IsFailure)
			{
				_logger.LogError("Failed to load claims for user {UserId}", usedId.Data!);
				return Result<AccessTokenDto>.Failure("An error occurred during creating access token");
			}
			var createAccessTokenDto = new CreateAccessTokenDto
			{
				UserId = usedId.Data!,
				Roles = rolesResult.Data!,
				AdditionalClaims = claimsResult.Data!
			};
			var accessToken=  _tokensService.GenerateAccessToken(createAccessTokenDto);
			if (accessToken.IsFailure) {
				return Result<AccessTokenDto>.Failure("An error occurred during creating access token");
			}
			return Result<AccessTokenDto>.Success(new AccessTokenDto { AccessToken= accessToken.Data! }, "Access token created successfully");
		}
		public async Task<Result<string>> RegisterAsync(RegisterRequestDto request)
		{

			var userCreationResult = await _userService.CreateUserAsync(request);
			if (userCreationResult.IsFailure)
			{
				return Result<string>.Failure(userCreationResult.Errors);
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
				_logger.LogWarning("Failed to send OTP for user {UserId}: {Error}", userId, otpResult.Errors.ToString());
				return Result<string>.Failure(otpResult.Errors);
			}
			//sms function
			return Result<string>.Success(data:userId!, message:"User registered successfully ");
		}
		public async Task<Result<int?>> ValidateOtpAsync(string phoneNumber, string code, OTPPurpose purpose)
		{
			var userFound = await _userService.GetUserByPhoneAsync(phoneNumber);
			if (userFound.IsFailure)
				return Result<int?>.Failure(userFound.Errors);

			var validated = await _otpService.VerifyOtpAsync(userFound.Data!.Id, code, purpose);
			if (!validated.IsSuccess)
				return Result<int?>.Failure(validated.Data, validated.Errors);
			var isValidated = await _userService.ConfirmPhoneNumber(userFound.Data!.Id);
			if (!isValidated.IsSuccess)
				return Result<int?>.Failure(isValidated.Errors);
			return Result<int?>.Success(null,"Phone number confirmed successfully.");
		}
		public async Task<Result> CreatePasswordResetOtpAsync(string phoneNumber)
		{
			var userfound = await _userService.GetUserByPhoneAsync(phoneNumber);
			if (userfound.IsFailure)
			{
				return Result.Failure(userfound.Errors);
			}
			var otpCreated= await _otpService.GenerateOtpAsync(userfound.Data!.Id,OTPPurpose.PasswordReset);
			if (otpCreated.IsFailure)
			{
				return Result.Failure(otpCreated.Errors);
			}
			//sms function
			return Result.Success("OTP sent successfully");
		}
		public async Task<Result<ResetTokWithRemainAttempts>> CreateResetPasswordTokenAsync(string PhoneNumber, string otp)
		{
			var userFound = await _userService.GetUserByPhoneAsync(PhoneNumber);
			if (userFound.IsFailure)
			{
				return Result<ResetTokWithRemainAttempts>.Failure(userFound.Errors);
			}
			var otpVerified = await _otpService.VerifyOtpAsync(userFound.Data!.Id, otp, OTPPurpose.PasswordReset);
			 var returnResult = new ResetTokWithRemainAttempts { };
			if (otpVerified.IsFailure)
			{
				returnResult.RemainingAttempts= otpVerified.Data;
				return Result<ResetTokWithRemainAttempts>.Failure(returnResult, otpVerified.Errors);
			}
			var resetToken = await _userService.CreateResetPasswordToken(userFound.Data.Id);
			returnResult.ResetToken = resetToken.Data!.Token;
			returnResult.RemainingAttempts = otpVerified.Data;

			return Result<ResetTokWithRemainAttempts>.Success(returnResult);
		}

		public async Task<Result> ChangePasswordAsync(ResetPasswordDto requestModel)
		{
		var userfound= await _userService.GetUserByPhoneAsync(requestModel.PhoneNumber);
			if (userfound.IsFailure)
				return Result.Failure(userfound.Errors);
			var passwordUpdated = await _userService.UpdateUserPassword(userfound.Data!.Id, requestModel.Password, requestModel.ResetToken);
			if (passwordUpdated.IsFailure) 
				return Result.Failure(passwordUpdated.Errors);
			var revokedAll = await _refreshTokenService.InvalidateAllForUserAsync(userfound.Data!.Id);
			if (revokedAll.IsFailure)
				return Result.Failure(revokedAll.Errors);
			return Result.Success("Password changed successfully");
		}
		public async Task<Result> ChangePasswordAsync(ChangePasswordDto requestModel)
		{
			var userFound = await _userService.GetUserByIdAsync(requestModel.UserId);
			if (userFound.IsFailure)
				return Result.Failure(userFound.Errors);
			var passwordChanged = await _userService.ChangePasswordAsync(userFound.Data!.Id,requestModel.OldPassword, requestModel.NewPassword);
			if (passwordChanged.IsFailure)
				return Result.Failure(passwordChanged.Errors);
			var revokedAll = await _refreshTokenService.InvalidateAllForUserAsync(userFound.Data!.Id);
			if (revokedAll.IsFailure)
				return Result.Failure(revokedAll.Errors);
			return Result.Success("Password changed successfully");
		}
		public async Task<Result> ResendPhoneVerificationOtp(string phoneNumber)
		{
		 var userFound = await _userService.GetUserByPhoneAsync(phoneNumber);
			if (userFound.IsFailure)
				return Result.Failure(userFound.Errors);
			var otpCreated = await _otpService.GenerateOtpAsync(userFound.Data!.Id, OTPPurpose.PhoneVerification);
			if (otpCreated.IsFailure)
				return Result.Failure(otpCreated.Errors);
			return Result.Success("Otp sent successfully");
		}
		public async Task<Result> RevokeAllRefreshTokens(string userId)
		{
			var userFound = await _userService.GetUserByIdAsync(userId);
			if (userFound.IsFailure)
				return Result.Failure(userFound.Errors);
			var areRevoked= await _refreshTokenService.InvalidateAllForUserAsync(userFound.Data!.Id);
			if (areRevoked.IsFailure)
				return Result.Failure(areRevoked.Errors);
			return Result.Success("User logged out from all devices ");
		}
	}
}

