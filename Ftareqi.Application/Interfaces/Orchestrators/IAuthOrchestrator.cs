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
		/// Invalidates a specific refresh token (logout from a single device).
		/// Note: does NOT invalidate access tokens already issued.
		/// </summary>
		Task<Result> RevokeRefreshTokenAsync(string refreshToken);

		/// <summary>
		/// Generates a new access token using a valid refresh token.
		/// Note: refresh token is not rotated (same refresh token remains valid until revoked/expired).
		/// </summary>
		Task<Result<AccessTokenDto>> RefreshAccessTokenAsync(string refreshToken);

		/// <summary>
		/// Registers a new user and sends phone verification OTP
		/// </summary>
		Task<Result<string>> RegisterAsync(RegisterRequestDto request);

		/// <summary>
		/// Verifies OTP, confirms phone number, and issues access+refresh tokens.
		/// </summary>
		Task<Result<TokensWithRemainAttempts>> VerifyOtpAndLoginAsync(string phoneNumber, string code, OTPPurpose purpose);

		/// <summary>
		/// Verifies OTP and confirms phone number.
		/// Warning: this method always attempts to confirm the phone number after OTP verification.
		/// </summary>
		Task<Result<int?>> VerifyOtpAndConfirmPhoneAsync(string phoneNumber, string code, OTPPurpose purpose);

		/// <summary>
		/// Requests (generates) an OTP for a given purpose (phone verification, password reset, etc.).
		/// The SMS sending step may be implemented by ISmsService depending on environment.
		/// </summary>
		Task<Result> RequestOtpAsync(string phoneNumber, OTPPurpose purpose);

		/// <summary>
		/// Verifies password reset OTP and creates a password reset token.
		/// </summary>
		Task<Result<ResetTokWithRemainAttempts>> CreatePasswordResetTokenFromOtpAsync(string phoneNumber, string otp);

		/// <summary>
		/// Resets user password using a password reset token.
		/// Side-effects: revokes all refresh tokens and deactivates all FCM tokens.
		/// </summary>
		Task<Result> ResetPasswordAsync(ResetPasswordDto requestModel);

		/// <summary>
		/// Changes user password (requires current password).
		/// Side-effects: revokes all refresh tokens and deactivates all FCM tokens.
		/// </summary>
		Task<Result> ChangePasswordWithCurrentPasswordAsync(ChangePasswordDto requestModel);

		/// <summary>
		/// Logs the user out from all devices.
		/// Side-effects: revokes all refresh tokens and deactivates all FCM tokens.
		/// Note: does NOT invalidate access tokens already issued.
		/// </summary>
		Task<Result> LogoutAllDevicesAsync(string userId);
	}
}