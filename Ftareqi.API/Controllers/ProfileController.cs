using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.Profile;
using Ftareqi.Application.DTOs.User;
using Ftareqi.Application.Interfaces.Orchestrators;
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
		public ProfileController(IUserOrchestrator userOrchestrator, IDriverOrchestrator driverOrchestrator)
		{
			_userOrchestrator = userOrchestrator;
			_driverOrchestrator = driverOrchestrator;
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
				Data= result.Data
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
				Data= result.Data
			});
		}
		[HttpGet("car/{driverId}")]

		public async Task<ActionResult<ApiResponse<CarProfileResponseDto>>> GetCarProfile([FromRoute] int driverId)
		{
			if (driverId <= 0)
				return BadRequest(new ApiResponse
				{
					Message = "invalid input",
					Errors = ["invalid input"],
					Success = false
				});
			var car= await _driverOrchestrator.GetCarByDriverProfileId(driverId);
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

	}
}
