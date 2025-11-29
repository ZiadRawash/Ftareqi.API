using FluentValidation;
using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.Interfaces.Orchestrators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace Ftareqi.API.Controller
{
	[Route("api/[controller]")]
	[ApiController]
	public class DriverRegistrationController : ControllerBase
	{
		private readonly IDriverOrchestrator _driverService;
		private readonly IValidator<DriverProfileReqDto> _validator;

		public DriverRegistrationController(
			IDriverOrchestrator driverService,
			IValidator<DriverProfileReqDto> validator)
		{
			_driverService = driverService;
			_validator = validator;
		}

		[HttpPost]
		public async Task<ActionResult<ApiResponse<string?>>> CreateDriverProfile([FromForm] DriverProfileReqDto dto)
		{
			// 1. Validate DTO
			var validationResult = await _validator.ValidateAsync(dto);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BadRequest(new ApiResponse { Success = false, Errors = errors });
			}


			var result = await _driverService.CreateDriverProfile(dto);

			if (result.IsFailure)
				return BadRequest(new ApiResponse { Success = false, Errors = result.Errors });

			return Ok(new ApiResponse<string>
			{
				Message = result.Message,
				Success = result.IsSuccess,
				Errors = result.Errors,
				Data = result.Data

			});
		}

		// Optional: retrieve driver profile
		//[HttpGet("{userId}")]
		//public async Task<IActionResult> GetDriverProfile(string userId)
		//{
		//	var profile = await _driverService.GetDriverProfileByUserId(userId);
		//	if (profile == null)
		//		return NotFound();
		//	return Ok(profile);
		//}
	}
}
