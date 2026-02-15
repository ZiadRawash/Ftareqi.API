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
		private readonly IWalletService _walletService;
		private readonly IUserService _userService;
		private readonly ITokensService _tokensService;
		private readonly IOtpService _otpService;
		private readonly IRefreshTokenService _refreshTokenService;
		private readonly ILogger<AuthOrchestrator> _logger;
		private readonly IUserClaimsService _userClaimsService;

		public AuthOrchestrator(IUserService userService,
			ITokensService tokensService,
			ILogger<AuthOrchestrator> logger,
			IOtpService otpService,
			IRefreshTokenService refreshTokenService,
			IUserClaimsService userClaimsService,
			IWalletService walletService)
		{
			_refreshTokenService = refreshTokenService;
			_otpService = otpService;
			_userService = userService;
			_tokensService = tokensService;
			_userClaimsService = userClaimsService;
			_logger = logger;
			_walletService = walletService;
		}

		//  helper method to generate tokens for a user
		private async Task<Result<TokensDto>> GenerateTokensForUserAsync(string userId)
		{
			var rolesResult = await _userClaimsService.GetUserRolesAsync(userId);
			if (rolesResult.IsFailure)
			{
				_logger.LogError("Failed to load roles for user {UserId}", userId);
				return Result<TokensDto>.Failure("An error occurred during token generation");
			}

			var claimsResult = await _userClaimsService.GetUserClaimsAsync(userId);
			if (claimsResult.IsFailure)
			{
				_logger.LogError("Failed to load claims for user {UserId}", userId);
				return Result<TokensDto>.Failure("An error occurred during token generation");
			}

			var createAccessTokenDto = new CreateAccessTokenDto
			{
				UserId = userId,
				Roles = rolesResult.Data!,
				AdditionalClaims = claimsResult.Data!
			};

			var accessTokenResult = _tokensService.GenerateAccessToken(createAccessTokenDto);
			if (accessTokenResult.IsFailure)
			{
				_logger.LogError("Failed to generate access token for user {UserId}", userId);
				return Result<TokensDto>.Failure("An error occurred during token generation");
			}

			var refreshTokenResult = _tokensService.GenerateRandomToken();
			if (refreshTokenResult.IsFailure)
			{
				_logger.LogError("Failed to generate refresh token for user {UserId}", userId);
				return Result<TokensDto>.Failure("An error occurred during token generation");
			}

			var tokenCreated = await _refreshTokenService.CreateAsync(userId, refreshTokenResult.Data!);
			if (tokenCreated.IsFailure)
			{
				_logger.LogError("Failed to store refresh token for user {UserId}", userId);
				return Result<TokensDto>.Failure("Error happened while creating refresh token");
			}

			_logger.LogInformation("Tokens generated successfully for user {UserId} with {RoleCount} roles and {ClaimCount} claims",
				userId, rolesResult.Data!.Count(), claimsResult.Data!.Count);

			return Result<TokensDto>.Success(new TokensDto
			{
				AccessToken = accessTokenResult.Data,
				RefreshToken = tokenCreated.Data!.Token
			});
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

			var tokensResult = await GenerateTokensForUserAsync(user!.Id);
			if (tokensResult.IsFailure)
			{
				return tokensResult;
			}

			_logger.LogInformation("User {UserId} logged in successfully", user.Id);

			return Result<TokensDto>.Success(tokensResult.Data!, "Login successful");
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
			var usedId = await _refreshTokenService.GetUserFromRefreshTokenAsync(refreshToken);
			if (usedId.IsFailure)
			{
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

			var accessToken = _tokensService.GenerateAccessToken(createAccessTokenDto);
			if (accessToken.IsFailure)
			{
				return Result<AccessTokenDto>.Failure("An error occurred during creating access token");
			}

			return Result<AccessTokenDto>.Success(new AccessTokenDto { AccessToken = accessToken.Data! }, "Access token created successfully");
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

			var roleAdded = await _userClaimsService.AddRolesAsync(userId!, new[] { Roles.User });
			if (roleAdded.IsSuccess)
			{
				_logger.LogInformation("role _User_ has been added successfully to {UserId}", userId);
			}

			// Generate and send OTP for phone verification
			var otpResult = await _otpService.GenerateOtpAsync(userId!, OTPPurpose.PhoneVerification);
			if (otpResult.IsFailure)
			{
				_logger.LogWarning("Failed to send OTP for user {UserId}: {Error}", userId, otpResult.Errors.ToString());
				return Result<string>.Failure(otpResult.Errors);
			}
		     await _walletService.CreateWalletAsync(userId!);
			//sms function
			return Result<string>.Success(data: userId!, message: "User registered successfully");
		}

		//  validate OTP and with tokens return
		public async Task<Result<TokensWithRemainAttempts>> ValidateOtpAndLoginAsync(string phoneNumber, string code, OTPPurpose purpose)
		{
			
			var userFound = await _userService.GetUserByPhoneAsync(phoneNumber);
			if (userFound.IsFailure)
				return Result<TokensWithRemainAttempts>.Failure(userFound.Errors);

			var validated = await _otpService.VerifyOtpAsync(userFound.Data!.Id, code, purpose);
			if (!validated.IsSuccess)
				return Result<TokensWithRemainAttempts>.Failure(new TokensWithRemainAttempts { RemainingAttempts= validated .Data}, validated.Errors);

			var isValidated = await _userService.ConfirmPhoneNumber(userFound.Data!.Id);
			if (!isValidated.IsSuccess)
				return Result<TokensWithRemainAttempts>.Failure(isValidated.Errors);

			var tokensResult = await GenerateTokensForUserAsync(userFound.Data!.Id);
			if (tokensResult.IsFailure)
			{
				return Result<TokensWithRemainAttempts>.Failure(tokensResult.Errors);
			}

			_logger.LogInformation("User {UserId} verified phone and logged in successfully", userFound.Data!.Id);

			return Result<TokensWithRemainAttempts>.Success(new TokensWithRemainAttempts { AccessToken=tokensResult.Data!.AccessToken, RefreshToken=tokensResult.Data.RefreshToken} , "Phone verified and logged in successfully");
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

			return Result<int?>.Success(null, "Phone number confirmed successfully.");
		}

		// Unified method for sending OTP for any purpose
		public async Task<Result> SendOtpAsync(string phoneNumber, OTPPurpose purpose)
		{
			var userfound = await _userService.GetUserByPhoneAsync(phoneNumber);
			if (userfound.IsFailure)
			{
				return Result.Failure(userfound.Errors);
			}

			var otpCreated = await _otpService.GenerateOtpAsync(userfound.Data!.Id, purpose);
			if (otpCreated.IsFailure)
			{
				return Result.Failure(otpCreated.Errors);
			}

			_logger.LogInformation("OTP sent successfully for user {UserId} with purpose {Purpose}", userfound.Data.Id, purpose);

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
				returnResult.RemainingAttempts = otpVerified.Data;
				return Result<ResetTokWithRemainAttempts>.Failure(returnResult, otpVerified.Errors);
			}

			var resetToken = await _userService.CreateResetPasswordToken(userFound.Data.Id);
			returnResult.ResetToken = resetToken.Data!.Token;
			returnResult.RemainingAttempts = otpVerified.Data;

			return Result<ResetTokWithRemainAttempts>.Success(returnResult);
		}

		public async Task<Result> ChangePasswordAsync(ResetPasswordDto requestModel)
		{
			var userfound = await _userService.GetUserByPhoneAsync(requestModel.PhoneNumber);
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

			var passwordChanged = await _userService.ChangePasswordAsync(userFound.Data!.Id, requestModel.OldPassword, requestModel.NewPassword);
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

			var areRevoked = await _refreshTokenService.InvalidateAllForUserAsync(userFound.Data!.Id);
			if (areRevoked.IsFailure)
				return Result.Failure(areRevoked.Errors);

			return Result.Success("User logged out from all devices");
		}
	}
}