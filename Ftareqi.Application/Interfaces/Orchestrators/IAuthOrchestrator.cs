using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Orchestrators
{
	/// <summary>
	/// Orchestrates authentication workflows combining user management, JWT, and notifications
	/// </summary>
	public interface IAuthOrchestrator
	{
		/// <summary>
		/// Handles complete registration workflow: validation, user creation, token generation, welcome notification
		/// </summary>
		Task<Result> RegisterAsync(RegisterRequestDto request);

		/// <summary>
		/// Handles complete login workflow: validation, credential check, token generation, activity logging
		/// </summary>
		Task<Result<TokensDto>> LoginAsync(LoginRequestDto request);

		/// <summary>
		/// Handles token refresh workflow
		/// </summary>
		 Task<Result<AccessTokenDto>> RefreshAccessToken(string refreshToken);

		/// <summary>
		/// Handles logout workflow: token invalidation, activity logging
		/// </summary>
		Task<Result> LogoutAsync(string refreshToken);

		/// <summary>
		/// Handles password reset workflow: validation, token generation, email sending
		/// </summary>
		Task<Result> CreatePasswordResetOtpAsync(string phoneNumber);

		/// <summary>
		/// Handles password reset confirmation workflow
		/// </summary>
		Task<Result<ResetTokWithRemainAttempts>> CreateResetPasswordTokenAsync(string PhoneNumber, string otp);
		Task<Result> ChangePasswordAsync(ResetPasswordDto requestModel);
		Task<Result> ChangePasswordAsync(ChangePasswordDto requestModel);
		Task<Result<int?>> ValidateOtpAsync(string userId, string code, OTPPurpose purpose);
		Task<Result> ResendPhoneVerificationOtp(string phoneNumber);
		Task<Result> RevokeAllRefreshTokens(string userId);
	}
}
