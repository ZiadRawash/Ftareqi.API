using FluentValidation;
using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.Profile;
using Ftareqi.Application.DTOs.User;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace Ftareqi.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class ProfileController : ControllerBase
	{
		private readonly IUserOrchestrator _userOrchestrator;
		private readonly IDriverOrchestrator _driverOrchestrator;
		private readonly IValidator<ProfileImageReqDto> _profileImageReqDtoValidator;
		public ProfileController(IUserOrchestrator userOrchestrator, IDriverOrchestrator driverOrchestrator, IValidator<ProfileImageReqDto> ProfileImageReqDtoValidator)
		{
			_userOrchestrator = userOrchestrator;
			_driverOrchestrator = driverOrchestrator;
			_profileImageReqDtoValidator = ProfileImageReqDtoValidator;
		}
		[HttpGet()]
		public async Task<ActionResult<ApiResponse<ProfileResponseDto>>> GetProfile()
		{
			var id = User.GetUserId();
			if (id == null)
				return NotFound();
			var result = await _userOrchestrator.GetProfile(id);
			if (result.IsFailure)
				return BadRequest(new ApiResponse
				{
					Errors = result.Errors,
					Success = result.IsSuccess,
					Message = result.Message
				});
			return Ok(new ApiResponse<ProfileResponseDto>
			{
				Errors = result.Errors,
				Success = result.IsSuccess,
				Message = result.Message,
				Data = result.Data
			});
		}
		[HttpGet("driver")]
		public async Task<ActionResult<ApiResponse<DriverProfileResponse>>> GetDriverProfile()
		{
			var id = User.GetUserId();
			if (id == null)
				return NotFound();
			var result = await _driverOrchestrator.GetDriverProfile(id);
			if (result.IsFailure)
				return BadRequest(new ApiResponse
				{
					Errors = result.Errors,
					Success = result.IsSuccess,
					Message = result.Message
				});
			return Ok(new ApiResponse<DriverProfileResponse>
			{
				Errors = result.Errors,
				Success = result.IsSuccess,
				Message = result.Message,
				Data = result.Data
			});
		}
		[HttpGet("driver/car")]

		public async Task<ActionResult<ApiResponse<CarProfileResponseDto>>> GetCarProfile()
		{
			var id = User.GetUserId();
			if (id == null)
				return NotFound();

			var car = await _driverOrchestrator.GetCarProfile(id);
			if (car.IsFailure)
				return BadRequest(new ApiResponse
				{
					Errors = car.Errors,
					Success = car.IsSuccess,
					Message = car.Message
				});
			return Ok(new ApiResponse<CarProfileResponseDto>
			{
				Errors = car.Errors,
				Success = car.IsSuccess,
				Message = car.Message,
				Data = car.Data
			});
		}
		[HttpPost("image")]
		public async Task<ActionResult<ApiResponse>> UploadProfileImage([FromForm] ProfileImageReqDto imageReqDto)
		{
			var id = User.GetUserId();
			if (id == null) return NotFound();

			var response = new ApiResponse();
			var validationResult = _profileImageReqDtoValidator.Validate(imageReqDto);

			if (!validationResult.IsValid)
			{
				response.Message = "Validation Error";
				response.Success = false;
				response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BadRequest(response);
			}

			var result = await _userOrchestrator.UploadProfileImage(id, imageReqDto);

			response.Message = result.Message;
			response.Success = result.IsSuccess;
			response.Errors = result.IsFailure ? result.Errors : null;

			return result.IsSuccess ? Ok(response) : BadRequest(response);
		}

		[HttpPut("image")]
		public async Task<ActionResult<ApiResponse>> UpdateProfileImage([FromForm] ProfileImageReqDto imageReqDto)
		{
			var id = User.GetUserId();
			var response = new ApiResponse();
			var validationResult = _profileImageReqDtoValidator.Validate(imageReqDto);
			if (!validationResult.IsValid)
			{
				response.Message = "Validation Error";
				response.Success = false;
				response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BadRequest(response);
			}
			var result = await _userOrchestrator.UpdateProfileImage(id, imageReqDto);
			response.Message = result.Message;
			response.Success = result.IsSuccess;
			response.Errors = result.IsFailure ? result.Errors : null;
			return result.IsSuccess ? Ok(response) : BadRequest(response);
		}
	}
}
