using FluentValidation;
using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Validators.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ftareqi.API.Controller
{

	[Route("api/[controller]")]
	[ApiController]
	[Consumes("application/json")]
	public class AuthController : ControllerBase
	{
		private readonly IValidator<RegisterRequestDto> _registerRequestDtoValidator;
		private readonly IAuthOrchestrator _authOrchestrator;


		public AuthController(IAuthOrchestrator authOrchestrator, IValidator<RegisterRequestDto> registerRequestDtoValidator)
		{
			_authOrchestrator = authOrchestrator;
			_registerRequestDtoValidator = registerRequestDtoValidator;
		}

		[HttpPost("register")]
		public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterRequestDto model)
		{

			var validation = await _registerRequestDtoValidator.ValidateAsync(model);
			if (!validation.IsValid)
			{
				var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
				return BadRequest(new ApiResponse { Success = false, Errors = errors });
			}
			var result = await _authOrchestrator.RegisterAsync(model);
			return Unauthorized(new ApiResponse
				{
					Message = result.Message,
					Errors = result.Errors,
					Success = result.IsSuccess,
				}
			);
		}

		[HttpPost("login")]
		public async Task<ActionResult<ApiResponse<TokensDto>>> Login([FromBody] LoginRequestDto request)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage)
					.ToList();

				return BadRequest(new ApiResponse<TokensDto>
				{
					Success = false,
					Errors = errors,
					Message = "Invalid request data"
				});
			}
			var result = await _authOrchestrator.LoginAsync(request);

			if (result.IsFailure)
			{
				return Unauthorized( new ApiResponse<TokensDto>
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

		[HttpPost("verify-phonenumber")]
		public async Task<ActionResult<ApiResponse>> VerifyPhone([FromBody] VerifyOtpRequestDto request)
		{
			var result = await _authOrchestrator.ValidateOtpAsync(request.PhoneNumber, request.OtpCode, Domain.Enums.OTPPurpose.PhoneVerification);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Message = result.Message,
					Success = result.IsSuccess,
					Errors = result.Errors,
					
				});
			}
			return Ok(new ApiResponse
			{
				Message = result.Message,
				Success = result.IsSuccess,
				Errors= result.Errors,
			});
		}

		[HttpPost("logout")]
		public async Task<ActionResult<ApiResponse>> logOut([FromBody] string refreshToken) { 
		
			var result= await _authOrchestrator.LogoutAsync(refreshToken);
			if (result.IsFailure)
				return BadRequest(new ApiResponse
				{
					Message = result.Message,
					Success = result.IsSuccess,
					Errors = result.Errors,
				});
			return Ok(new ApiResponse {
				Message = result.Message,
				Success = result.IsSuccess,
				Errors = result.Errors,
			});
		}
		[HttpPost("refresh-token")]
		public async Task<ActionResult<ApiResponse<string>>> RefreshToken([FromBody] string refreshToken)
		{

			var result = await _authOrchestrator.RefreshTokenAsync(refreshToken);
			
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
				Data = result.Data
			
			});
		}
	}
}
