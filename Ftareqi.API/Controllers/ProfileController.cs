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
	/// <summary>
	/// Provides endpoints for retrieving and managing user and driver profile data.
	/// </summary>
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

		/// <summary>
		/// Gets the authenticated user's profile.
		/// </summary>
		/// <returns>
		/// Returns the current user's profile details.
		/// </returns>
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

		/// <summary>
		/// Gets the authenticated user's driver profile.
		/// </summary>
		/// <returns>
		/// Returns driver profile details for the current authenticated user.
		/// </returns>
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

		/// <summary>
		/// Gets the public profile for a driver by user id.
		/// </summary>
		/// <param name="userId">The target driver's user identifier.</param>
		/// <returns>
		/// Returns public driver information including basic profile and car details.
		/// </returns>
		[HttpGet("driver/{userId}")]
		public async Task<ActionResult<ApiResponse<PublicDriverProfileDto>>> GetPublicDriverProfile (string userId)
		{
			if (userId == null) 
				return NotFound();
		var result= await _driverOrchestrator.GetPublicDriverProfile(userId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Errors = result.Errors,
					Message = result.Message,
					Success = result.IsSuccess
				});
			}
			return Ok(new ApiResponse<PublicDriverProfileDto>
			{
				Errors = result.Errors,
				Data = result.Data,
				Success = result.IsSuccess,
				Message = result.Message,
			});


		}

		/// <summary>
		/// Gets the authenticated driver's car profile.
		/// </summary>
		/// <returns>
		/// Returns car profile details for the current authenticated driver's profile.
		/// </returns>
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

		/// <summary>
		/// Uploads a profile image for the authenticated user.
		/// </summary>
		/// <param name="imageReqDto">The profile image upload request payload.</param>
		/// <returns>
		/// Returns success when the image is uploaded, otherwise validation or upload errors.
		/// </returns>
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

		/// <summary>
		/// Updates the profile image for the authenticated user.
		/// </summary>
		/// <param name="imageReqDto">The profile image update request payload.</param>
		/// <returns>
		/// Returns success when the image is updated, otherwise validation or update errors.
		/// </returns>
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
