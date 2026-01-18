using FluentValidation;
using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ftareqi.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly IValidator<RegisterRequestDto> _registerRequestDtoValidator;
		private readonly IAuthOrchestrator _authOrchestrator;
		private readonly IValidator<ChangePasswordDto> _changePasswordValidator;

		public AuthController(
			IAuthOrchestrator authOrchestrator,
			IValidator<RegisterRequestDto> registerRequestDtoValidator,
			IValidator<ChangePasswordDto> changePasswordValidator)
		{
			_authOrchestrator = authOrchestrator;
			_registerRequestDtoValidator = registerRequestDtoValidator;
			_changePasswordValidator = changePasswordValidator;
		}

		/// <summary>
		/// Registers a new user account
		/// </summary>
		[HttpPost("register")]
		public async Task<ActionResult<ApiResponse<UserIdDto>>> Register([FromBody] RegisterRequestDto model)
		{
			var validation = await _registerRequestDtoValidator.ValidateAsync(model);
			if (!validation.IsValid)
			{
				var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
				return BadRequest(new ApiResponse { Success = false, Errors = errors });
			}

			var result = await _authOrchestrator.RegisterAsync(model);

			if (result.IsFailure)
			{
				return Unauthorized(new ApiResponse
				{
					Success = false,
					Errors = result.Errors,
					Message = result.Message,
				});
			}

			return Ok(new ApiResponse<UserIdDto>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = new UserIdDto
				{
					Id = result.Data
				}
			});
		}

		/// <summary>
		/// Authenticates a user and returns access tokens
		/// </summary>
		[HttpPost("login")]
		public async Task<ActionResult<ApiResponse<TokensDto>>> Login([FromBody] LoginRequestDto request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var result = await _authOrchestrator.LoginAsync(request);

			if (result.IsFailure)
			{
				return Unauthorized(new ApiResponse<TokensDto>
				{
					Success = false,
					Errors = result.Errors,
					Message = "Login failed."
				});
			}

			return Ok(new ApiResponse<TokensDto>
			{
				Success = true,
				Data = new TokensDto
				{
					RefreshToken = result.Data!.RefreshToken,
					AccessToken = result.Data.AccessToken
				},
				Message = "Login successful."
			});
		}

		/// <summary>
		/// Logs out a user by invalidating their refresh token
		/// </summary>
		[HttpPost("logout")]
		public async Task<ActionResult<ApiResponse>> Logout([FromBody] RefreshToken refreshToken)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var result = await _authOrchestrator.LogoutAsync(refreshToken.Token);
			if (result.IsFailure)
				return BadRequest(new ApiResponse
				{
					Message = result.Message,
					Success = result.IsSuccess,
					Errors = result.Errors,
				});

			return Ok(new ApiResponse
			{
				Message = result.Message,
				Success = result.IsSuccess,
				Errors = result.Errors,
			});
		}

		/// <summary>
		/// Logs out the user from all devices by revoking all their active refresh tokens
		/// </summary>
		[HttpPost("logout/all")]
		public async Task<ActionResult<ApiResponse>> LogoutAll([FromBody] RefreshToken refreshToken)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var result = await _authOrchestrator.RevokeAllRefreshTokens(refreshToken.Token);

			if (result.IsFailure)
				return BadRequest(new ApiResponse
				{
					Message = result.Message,
					Success = result.IsSuccess,
					Errors = result.Errors,
				});

			return Ok(new ApiResponse
			{
				Message = result.Message,
				Success = result.IsSuccess,
				Errors = result.Errors,
			});
		}

		/// <summary>
		/// Generates a new access token using a refresh token
		/// </summary>
		[HttpPost("token/refresh")]
		public async Task<ActionResult<ApiResponse<string>>> RefreshToken([FromBody] RefreshToken refreshToken)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var result = await _authOrchestrator.RefreshAccessToken(refreshToken.Token);

			if (result.IsFailure)
				return BadRequest(new ApiResponse
				{
					Message = result.Message,
					Success = result.IsSuccess,
					Errors = result.Errors,
				});

			return Ok(new ApiResponse<string>
			{
				Message = result.Message,
				Success = result.IsSuccess,
				Errors = result.Errors,
				Data = result.Data!.AccessToken
			});
		}

		/// <summary>
		/// Verifies user's phone number using OTP code and logs them in automatically
		/// </summary>
		[HttpPost("phone/verify")]
		public async Task<ActionResult<ApiResponse<TokensWithRemainAttempts>>> VerifyPhoneAndLogin([FromBody] PhoneWithResetOtpDto request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var result = await _authOrchestrator.ValidateOtpAndLoginAsync(
				request.PhoneNumber,
				request.otp,
				OTPPurpose.PhoneVerification);

			var model = new ApiResponse<TokensWithRemainAttempts>
			{
				Message = result.Message,
				Success = result.IsSuccess,
				Errors = result.Errors,
				Data = result.Data
			};
			if (result.IsFailure)
				return BadRequest(model);
			return Ok(model);
		}
		/// <summary>
		/// Resends phone verification OTP to user's phone number
		/// </summary>
		[HttpPost("phone/resend-otp")]
		public async Task<ActionResult<ApiResponse>> ResendVerificationOtp([FromBody] PhoneNumberRequestDto model)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var result = await _authOrchestrator.SendOtpAsync(model.PhoneNumber, OTPPurpose.PhoneVerification);

			if (result.IsFailure)
				return BadRequest(new ApiResponse
				{
					Message = result.Message,
					Success = result.IsSuccess,
					Errors = result.Errors,
				});

			return Ok(new ApiResponse
			{
				Message = result.Message,
				Success = result.IsSuccess,
				Errors = result.Errors,
			});
		}

		/// <summary>
		/// Initiates password-reset process by sending OTP
		/// </summary>
		[HttpPost("password/reset/request-otp")]
		public async Task<ActionResult<ApiResponse>> RequestPasswordReset([FromBody] PhoneNumberRequestDto model)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var result = await _authOrchestrator.SendOtpAsync(model.PhoneNumber, OTPPurpose.PasswordReset);

			if (result.IsFailure)
				return BadRequest(new ApiResponse
				{
					Message = result.Message,
					Success = result.IsSuccess,
					Errors = result.Errors,
				});

			return Ok(new ApiResponse
			{
				Message = result.Message,
				Success = result.IsSuccess,
				Errors = result.Errors,
			});
		}

		/// <summary>
		/// Validates password reset OTP and generates reset token
		/// </summary>
		[HttpPost("password/reset/verify-otp")]
		public async Task<ActionResult<ApiResponse<ResetTokWithRemainAttempts>>> VerifyResetPasswordOtp([FromBody] PhoneWithResetOtpDto model)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());


			var tokenCreated = await _authOrchestrator.CreateResetPasswordTokenAsync(
				model.PhoneNumber,
				model.otp);

			if (tokenCreated.IsFailure)
			{
				return BadRequest(new ApiResponse<ResetTokWithRemainAttempts>
				{
					Success = false,
					Errors = tokenCreated.Errors,
					Message = "Invalid request data",
					Data = tokenCreated.Data
				});
			}

			return Ok(new ApiResponse<ResetTokWithRemainAttempts>
			{
				Message = tokenCreated.Message,
				Success = tokenCreated.IsSuccess,
				Errors = tokenCreated.Errors,
				Data = tokenCreated.Data,
			});
		}

		/// <summary>
		/// Resets user password using reset token
		/// </summary>
		[HttpPost("password/reset")]
		public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordDto model)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var result = await _authOrchestrator.ChangePasswordAsync(model);

			if (result.IsFailure)
				return BadRequest(new ApiResponse
				{
					Message = result.Message,
					Success = result.IsSuccess,
					Errors = result.Errors,
				});

			return Ok(new ApiResponse
			{
				Message = result.Message,
				Success = result.IsSuccess,
				Errors = result.Errors,
			});
		}

		/// <summary>
		/// Changes the user's password by validating the current password and setting a new one
		/// </summary>
		[HttpPost("password/change")]
		public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordDto model)
		{
			var validation = await _changePasswordValidator.ValidateAsync(model);
			if (!validation.IsValid)
			{
				var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
				return BadRequest(new ApiResponse
				{
					Success = false,
					Errors = errors
				});
			}

			var result = await _authOrchestrator.ChangePasswordAsync(model);
			if (result.IsFailure)
				return BadRequest(new ApiResponse
				{
					Message = result.Message,
					Success = result.IsSuccess,
					Errors = result.Errors,
				});

			return Ok(new ApiResponse
			{
				Message = result.Message,
				Success = result.IsSuccess,
				Errors = result.Errors,
			});
		}
	}
}