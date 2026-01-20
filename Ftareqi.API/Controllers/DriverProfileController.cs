using FluentValidation;
using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Mappers;
using Ftareqi.Application.Validators.Car;
using Ftareqi.Application.Validators.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Ftareqi.API.Controllers
{
	[ApiController]
	public class DriverProfileController : ControllerBase
	{
		private readonly IDriverOrchestrator _driverOrchestrator;
		private readonly IValidator<DriverProfileReqDto> _DriverProfileReqDtoValidator;
		private readonly IValidator<CarReqDto> _CarReqDtoValidatorValidator;
		private readonly IValidator<DriverProfileUpdateReqDto> _driverProfileUpdateReqDtoValidator;
		private readonly IValidator<CarUpdateReqDto> _carUpdateReqDtoValidator;
		public DriverProfileController(
			IValidator<CarUpdateReqDto> carUpdateReqDtoValidator,
			IValidator<DriverProfileUpdateReqDto> DriverProfileUpdateReqDtoValidator,
			IDriverOrchestrator driverOrchestrator,
			IValidator<DriverProfileReqDto> driverValidator,
			IValidator<CarReqDto> carValidator)
		{
			_driverProfileUpdateReqDtoValidator = DriverProfileUpdateReqDtoValidator;
			_driverOrchestrator = driverOrchestrator;
			_DriverProfileReqDtoValidator = driverValidator;
			_CarReqDtoValidatorValidator = carValidator;
			_carUpdateReqDtoValidator = carUpdateReqDtoValidator;
		}

		/// <summary>
		///	Creates Driver Profile
		/// </summary>
		[HttpPost("/api/users/{userId}/driver-profile")]
		public async Task<ActionResult<ApiResponse>> CreateDriverProfile([FromRoute] string userId,[FromForm] DriverProfileReqDto request)
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
				
				return BadRequest(new ApiResponse {Errors = result.Errors ,Success= result.IsSuccess, Message=result.Message });
			}

			return Ok(new ApiResponse { Errors = result.Errors, Success = result.IsSuccess, Message = result.Message });
		}
		/// <summary>
		///	Creates car for driver-Profile
		/// </summary>

		[HttpPost("/api/users/{userId}/driver-profile/car")]
		public async Task<ActionResult<ApiResponse>> AddCarToDriverProfile([FromRoute] string userId,[FromForm] CarReqDto request)
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
				return BadRequest(new ApiResponse { Errors = result.Errors, Success = result.IsSuccess, Message = result.Message });
			}
			return Ok(new ApiResponse { Errors = result.Errors, Success = result.IsSuccess, Message = result.Message });
		}
		/// <summary>
		///	Update Driver Profile
		/// </summary>

		[HttpPatch("/api/users/{userId}/driver-profile")]
		public async Task<ActionResult<ApiResponse>> UpdateDriverProfile([FromRoute] string userId,[FromForm] DriverProfileUpdateReqDto request)
		{
			var validationResult = await _driverProfileUpdateReqDtoValidator.ValidateAsync(request);

			if (!validationResult.IsValid || string.IsNullOrEmpty(userId))
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				if (string.IsNullOrEmpty(userId))
					errors.Add("UserId is required");

				return BadRequest(new ApiResponse
				{
					Success = false,
					Errors = errors
				});
			}

			var updateDto = request.ToUpdateDto(userId);
			var result = await _driverOrchestrator.UpdateDriverProfileAsync(updateDto);

			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Errors = result.Errors,
					Success = result.IsSuccess,
					Message = result.Message
				});
			}

			return Ok(new ApiResponse
			{
				Errors= result.Errors,
				Success = result.IsSuccess,
				Message = result.Message
			});
		}
		/// <summary>
		///	Update car 
		/// </summary>
		[HttpPatch("/api/users/{userId}/driver-profile/car")]
		public async Task<ActionResult<ApiResponse>> UpdateCarForDriverProfile([FromRoute] string userId, [FromForm] CarUpdateReqDto request)
		{
			var validationResult = await _carUpdateReqDtoValidator.ValidateAsync(request);

			if (!validationResult.IsValid || string.IsNullOrEmpty(userId))
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				if (string.IsNullOrEmpty(userId))
					errors.Add("UserId is required");

				return BadRequest(new ApiResponse
				{
					Success = false,
					Errors = errors
				});
			}
			var updateDto = request.ToUpdateDto(userId);
			var result = await _driverOrchestrator.UpdateCarAsync(updateDto);

			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Errors = result.Errors,
					Success = false,
					Message = result.Message
				});
			}

			return Ok(new ApiResponse
			{
				Success = true,
				Message = result.Message
			});
		}
	}
}