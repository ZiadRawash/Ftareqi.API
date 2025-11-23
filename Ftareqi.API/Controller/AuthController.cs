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
		[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK, "application/json")]
		[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest, "application/json")]
		[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict, "application/json")]
		[ProducesResponseType(typeof(ErrorResponse), 500, "application/problem+json")]
		public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterRequestDto model)
		{
			var validation = await _registerRequestDtoValidator.ValidateAsync(model);
			if (!validation.IsValid)
			{
				var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
				return BadRequest(new ApiResponse { Success = false, Errors = errors });
			}
			var result = await _authOrchestrator.RegisterAsync(model);
			return StatusCode(result.StatusCode,
				new ApiResponse
				{
					Message = result.Message,
					Errors = result.Errors,
					Success = result.IsSuccess,
				}
			);
		}
	}
}