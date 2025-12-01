using FluentValidation;
using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Validators.Car;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Ftareqi.Application.Mappers;

namespace Ftareqi.API.Controllers
{
	[ApiController]
	public class DriverProfileController : ControllerBase
	{
		private readonly IDriverOrchestrator _driverOrchestrator;
		private readonly IValidator<DriverProfileReqDto> _DriverProfileReqDtoValidator;
		private readonly IValidator<CarReqDto> _CarReqDtoValidatorValidator;
		public DriverProfileController(
			IDriverOrchestrator driverOrchestrator,
			IValidator<DriverProfileReqDto> driverValidator,
			IValidator<CarReqDto> carValidator)
		{
			_driverOrchestrator = driverOrchestrator;
			_DriverProfileReqDtoValidator = driverValidator;
			_CarReqDtoValidatorValidator = carValidator;
		}

		/// <summary>
		///	Creates Driver Profile
		/// </summary>
		[HttpPost("/api/users/{userId}/driver-profile")]
		public async Task<IActionResult> CreateDriverProfile([FromRoute] string userId,[FromForm] DriverProfileReqDto request)
		{
			var validationResult = await _DriverProfileReqDtoValidator.ValidateAsync(request);
			if (!validationResult.IsValid|| string.IsNullOrEmpty(userId))
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BadRequest(new ApiResponse { Success = false, Errors = errors ?? ["UserId is Required"] });
			}
			var profileDto = request.ToCreateDto(userId);
			var result = await _driverOrchestrator.CreateDriverProfileAsync(profileDto);

			if (result.IsFailure)
			{
				
				return BadRequest(new { errors = result.Errors });
			}

			return Ok(result.Data);
		}

		/// <summary>
		///	Creates car profile and connect it to driver profile
		/// </summary>
		[HttpPost("/api/users/{userId}/driver-profile/car")]
			public async Task<IActionResult> AddCarToDriverProfile([FromRoute] string userId,[FromForm] CarReqDto request)
		{
			var validationResult = await _CarReqDtoValidatorValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BadRequest(new ApiResponse { Success = false, Errors = errors });
			}
			var carModel = request.ToCreateDto(userId);
			var result = await _driverOrchestrator.CreateCarForDriverProfile(carModel);

			if (result.IsFailure)
			{	
				return BadRequest(new { errors = result.Errors });
			}
			return Ok(result.Data);
		}

	}
}