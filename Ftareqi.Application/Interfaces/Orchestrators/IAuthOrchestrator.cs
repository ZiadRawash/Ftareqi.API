using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Domain.Enums;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Orchestrators
{
	public interface IAuthOrchestrator
	{
		/// <summary>
		/// Authenticates user with phone number and password, returns access and refresh tokens
		/// </summary>
		Task<Result<TokensDto>> LoginAsync(LoginRequestDto request);

		/// <summary>
		/// Invalidates a specific refresh token (logout from single device)
		/// </summary>
		Task<Result> LogoutAsync(string refreshToken);

		/// <summary>
		/// Generates a new access token using a valid refresh token
		/// </summary>
		Task<Result<AccessTokenDto>> RefreshAccessToken(string refreshToken);

		/// <summary>
		/// Registers a new user and sends phone verification OTP
		/// </summary>
		Task<Result<string>> RegisterAsync(RegisterRequestDto request);

		/// <summary>
		/// Validates OTP and automatically logs user in, returning access and refresh tokens
		/// </summary>
		Task<Result<TokensWithRemainAttempts>> ValidateOtpAndLoginAsync(string phoneNumber, string code, OTPPurpose purpose);

		/// <summary>
		/// Validates OTP without generating tokens (for verification only)
		/// </summary>
		Task<Result<int?>> ValidateOtpAsync(string phoneNumber, string code, OTPPurpose purpose);

		/// <summary>
		/// Sends OTP for any purpose (phone verification, password reset, etc.)
		/// </summary>
		Task<Result> SendOtpAsync(string phoneNumber, OTPPurpose purpose);

		/// <summary>
		/// Verifies password reset OTP and creates a reset token
		/// </summary>
		Task<Result<ResetTokWithRemainAttempts>> CreateResetPasswordTokenAsync(string phoneNumber, string otp);

		/// <summary>
		/// Resets user password using reset token
		/// </summary>
		Task<Result> ChangePasswordAsync(ResetPasswordDto requestModel);

		/// <summary>
		/// Changes user password (requires old password)
		/// </summary>
		Task<Result> ChangePasswordAsync(ChangePasswordDto requestModel);

		/// <summary>
		/// Invalidates all refresh tokens for a user (logout from all devices)
		/// </summary>
		Task<Result> RevokeAllRefreshTokens(string userId);
	}
}