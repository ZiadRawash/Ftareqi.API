using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Constants;
using Ftareqi.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ftareqi.Application.Orchestrators
{
	/// <summary>
	/// Orchestrates authentication flows (login, OTP verification, password reset/change, refresh token operations).
	/// </summary>
	public class AuthOrchestrator : IAuthOrchestrator
	{
		private readonly IWalletService _walletService;
		private readonly IUserService _userService;
		private readonly ITokensService _tokensService;
		private readonly IOtpService _otpService;
		private readonly IRefreshTokenService _refreshTokenService;
		private readonly IFcmTokenService _fcmTokenService;
		private readonly ILogger<AuthOrchestrator> _logger;
		private readonly IUserClaimsService _userClaimsService;
		private readonly ISmsService _smsService;
		public AuthOrchestrator(IUserService userService,
			ITokensService tokensService,
			ILogger<AuthOrchestrator> logger,
			IOtpService otpService,
			IRefreshTokenService refreshTokenService,
			IFcmTokenService fcmTokenService,
			IUserClaimsService userClaimsService,
			IWalletService walletService,
			ISmsService smsService)
		{
			_refreshTokenService = refreshTokenService;
			_fcmTokenService = fcmTokenService;
			_otpService = otpService;
			_userService = userService;
			_tokensService = tokensService;
			_userClaimsService = userClaimsService;
			_logger = logger;
			_walletService = walletService;
			_smsService = smsService;
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

			var tokensResult = await IssueTokensForUserAsync(user!.Id);
			if (tokensResult.IsFailure)
			{
				return tokensResult;
			}

			_logger.LogInformation("User {UserId} logged in successfully", user.Id);

			return Result<TokensDto>.Success(tokensResult.Data!, "Login successful");
		}

		/// <summary>
		/// Revokes (invalidates) a single refresh token.
		/// Note: does not invalidate access tokens already issued.
		/// </summary>
		public async Task<Result> RevokeRefreshTokenAsync(string refreshToken)
		{
			var tokenFound = await _refreshTokenService.InvalidateAsync(refreshToken);
			if (tokenFound.IsFailure)
				return Result.Failure(tokenFound.Errors);
			return Result.Success("Refresh token has been revoked successfully");
		}

		/// <summary>
		/// Creates a new access token using a valid refresh token.
		/// Note: refresh token is not rotated.
		/// </summary>
		public async Task<Result<AccessTokenDto>> RefreshAccessTokenAsync(string refreshToken)
		{
			var userIdFromRefreshToken = await _refreshTokenService.GetUserFromRefreshTokenAsync(refreshToken);
			if (userIdFromRefreshToken.IsFailure)
			{
				return Result<AccessTokenDto>.Failure(userIdFromRefreshToken.Errors);
			}

			var rolesResult = await _userClaimsService.GetUserRolesAsync(userIdFromRefreshToken.Data!);
			if (rolesResult.IsFailure)
			{
				_logger.LogError("Failed to load roles for user {UserId}", userIdFromRefreshToken.Data!);
				return Result<AccessTokenDto>.Failure("An error occurred during creating access token");
			}

			var claimsResult = await _userClaimsService.GetUserClaimsAsync(userIdFromRefreshToken.Data!);
			if (claimsResult.IsFailure)
			{
				_logger.LogError("Failed to load claims for user {UserId}", userIdFromRefreshToken.Data!);
				return Result<AccessTokenDto>.Failure("An error occurred during creating access token");
			}

			var createAccessTokenDto = new CreateAccessTokenDto
			{
				UserId = userIdFromRefreshToken.Data!,
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
			if (roleAdded.IsFailure)
			{
				_logger.LogWarning("Failed to add role _User_ to {UserId}", userId);
				return Result<string>.Failure(roleAdded.Errors);
			}

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
			// var otpSent = await _smsService.SendSMS(userCreationResult.Data!.PhoneNumber, otpResult.Data!.Otp);
			// if (otpSent.IsFailure)
			// {
			// 	return Result<string>.Failure(otpSent.Errors);
			// }

			try
			{
				await _walletService.CreateWalletAsync(userId!);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Wallet creation failed for user {UserId}", userId);
				return Result<string>.Failure("User setup failed while creating wallet");
			}

			return Result<string>.Success(data: userId!, message: "User registered successfully");
		}

		/// <summary>
		/// Verifies an OTP, confirms the user's phone number, then issues access+refresh tokens.
		/// </summary>
		public async Task<Result<TokensWithRemainAttempts>> VerifyOtpAndLoginAsync(string phoneNumber, string code, OTPPurpose purpose)
		{
			var userFound = await _userService.GetUserByPhoneAsync(phoneNumber);
			if (userFound.IsFailure)
				return Result<TokensWithRemainAttempts>.Failure(userFound.Errors);

			var validated = await _otpService.VerifyOtpAsync(userFound.Data!.Id, code, purpose);
			if (!validated.IsSuccess)
				return Result<TokensWithRemainAttempts>.Failure(new TokensWithRemainAttempts { RemainingAttempts = validated.Data }, validated.Errors);

			var isValidated = await _userService.ConfirmPhoneNumber(userFound.Data!.Id);
			if (!isValidated.IsSuccess)
				return Result<TokensWithRemainAttempts>.Failure(isValidated.Errors);

			var tokensResult = await IssueTokensForUserAsync(userFound.Data!.Id);
			if (tokensResult.IsFailure)
			{
				return Result<TokensWithRemainAttempts>.Failure(tokensResult.Errors);
			}

			_logger.LogInformation("User {UserId} verified phone and logged in successfully", userFound.Data!.Id);

			return Result<TokensWithRemainAttempts>.Success(new TokensWithRemainAttempts { AccessToken = tokensResult.Data!.AccessToken, RefreshToken = tokensResult.Data.RefreshToken }, "Phone verified and logged in successfully");
		}

		/// <summary>
		/// Verifies an OTP and confirms the user's phone number.
		/// Warning: this method confirms the phone number after OTP verification.
		/// </summary>
		public async Task<Result<int?>> VerifyOtpAndConfirmPhoneAsync(string phoneNumber, string code, OTPPurpose purpose)
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

		/// <summary>
		/// Requests (generates) an OTP for a given purpose.
		/// Note: SMS sending may be enabled/disabled depending on ISmsService usage.
		/// </summary>
		public async Task<Result> RequestOtpAsync(string phoneNumber, OTPPurpose purpose)
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

			//var otpSent = await _smsService.SendSMS(userfound.Data!.PhoneNumber, otpCreated.Data!.Otp);
			//if (otpSent.IsFailure)
			//{
			//	return Result.Failure(otpSent.Errors);
			//}
			return Result.Success("OTP sent successfully");
		}

		/// <summary>
		/// Verifies password-reset OTP and generates a password reset token.
		/// </summary>
		public async Task<Result<ResetTokWithRemainAttempts>> CreatePasswordResetTokenFromOtpAsync(string phoneNumber, string otp)
		{
			var userFound = await _userService.GetUserByPhoneAsync(phoneNumber);
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
			if (resetToken.IsFailure || resetToken.Data is null)
			{
				return Result<ResetTokWithRemainAttempts>.Failure(resetToken.Errors);
			}

			returnResult.ResetToken = resetToken.Data!.Token;
			returnResult.RemainingAttempts = otpVerified.Data;

			return Result<ResetTokWithRemainAttempts>.Success(returnResult);
		}

		/// <summary>
		/// Resets the password using a reset token, then revokes all refresh tokens and deactivates all FCM tokens.
		/// </summary>
		public async Task<Result> ResetPasswordAsync(ResetPasswordDto requestModel)
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

			var fcmTokensDeactivated = await _fcmTokenService.DeactivateAll(userfound.Data!.Id);
			if (fcmTokensDeactivated.IsFailure)
				return Result.Failure(fcmTokensDeactivated.Errors);

			return Result.Success("Password changed successfully");
		}

		/// <summary>
		/// Changes the password using the current password, then revokes all refresh tokens and deactivates all FCM tokens.
		/// </summary>
		public async Task<Result> ChangePasswordWithCurrentPasswordAsync(ChangePasswordDto requestModel)
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

			var fcmTokensDeactivated = await _fcmTokenService.DeactivateAll(userFound.Data!.Id);
			if (fcmTokensDeactivated.IsFailure)
				return Result.Failure(fcmTokensDeactivated.Errors);

			return Result.Success("Password changed successfully");
		}

		/// <summary>
		/// Backwards-compatible wrapper for requesting a phone verification OTP.
		/// Prefer calling <see cref="RequestOtpAsync"/> with <see cref="OTPPurpose.PhoneVerification"/>.
		/// </summary>
		public async Task<Result> RequestPhoneVerificationOtpAsync(string phoneNumber)
		{
			return await RequestOtpAsync(phoneNumber, OTPPurpose.PhoneVerification);
		}

		/// <summary>
		/// Logs the user out from all devices by revoking all refresh tokens and deactivating all FCM tokens.
		/// Note: does not invalidate access tokens already issued.
		/// </summary>
		public async Task<Result> LogoutAllDevicesAsync(string userId)
		{
			var userFound = await _userService.GetUserByIdAsync(userId);
			if (userFound.IsFailure)
				return Result.Failure(userFound.Errors);

			var areRevoked = await _refreshTokenService.InvalidateAllForUserAsync(userFound.Data!.Id);
			if (areRevoked.IsFailure)
				return Result.Failure(areRevoked.Errors);

			var fcmTokensDeactivated = await _fcmTokenService.DeactivateAll(userFound.Data!.Id);
			if (fcmTokensDeactivated.IsFailure)
				return Result.Failure(fcmTokensDeactivated.Errors);


			return Result.Success("User logged out from all devices");
		}

		/// <summary>
		/// Issues access+refresh tokens for a user.
		/// </summary>
		private async Task<Result<TokensDto>> IssueTokensForUserAsync(string userId)
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
	}
}