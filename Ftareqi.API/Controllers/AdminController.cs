using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Application.DTOs.User;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Orchestrators;
using Ftareqi.Application.QueryEnums;
using Ftareqi.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ftareqi.API.Controllers
{
	[Authorize(Roles = Roles.Admin)]
	[Route("api/admin/")]
	[ApiController]
	public class AdminController : ControllerBase
	{
		private readonly IUserOrchestrator _userOrchestrator;
		private readonly IUserClaimsService _claimsService;
		public AdminController(IUserOrchestrator userOrchestrator,IUserClaimsService claimsService)
		{
			_claimsService = claimsService;
			_userOrchestrator = userOrchestrator;
		}
		[HttpGet("users")]
		public async Task<ActionResult<ApiResponse<PaginatedResponse<UserDriveStatusDto>>>> GetUsers([FromQuery] UserQueryDto queryModel)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState.ToApiResponse());
			}

			var result = await _userOrchestrator.GetUserWithDriverStatus(queryModel);

			if (!result.IsSuccess)
			{
				return BadRequest(new ApiResponse{
				Errors = result.Errors,
				Message = result.Message,
				Success=false	
				});
			}
			return Ok(new ApiResponse<PaginatedResponse<UserDriveStatusDto>>
			{

				Errors = result.Errors,
				Message = result.Message,
				Success=result.IsSuccess,
				Data=result.Data		
			});
		}
		[HttpGet("users/{userId}")]
		public async Task<ActionResult<ApiResponse<UserWithRolesDto>>> GetUserDetails(string userId)
		{	
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var result= await _userOrchestrator.GetUserDetails(userId);
			if (!result.IsSuccess)
				return BadRequest(new ApiResponse
				{
					Errors = result.Errors,
					Message = result.Message,
					Success = false
				});
			return Ok(new ApiResponse<UserWithRolesDto>
			{
				Errors = result.Errors,
				Message = result.Message,
				Success = result.IsSuccess,
				Data = result.Data
			});
		}
		[HttpDelete("users/{userId}/remove-role/{role}")]
		public async Task<ActionResult<ApiResponse>> RemoveRole(string userId,RolesFields role)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());
			var returnResult = await _claimsService.RemoveRoleAsync(userId, role.ToString());
			if (!returnResult.IsSuccess)
				return  BadRequest(new ApiResponse
				{
					Errors = returnResult.Errors,
					Message= returnResult.Message,
					Success= returnResult.IsSuccess,
				});
			return Ok(new ApiResponse
			{
				Errors = returnResult.Errors,
				Message = returnResult.Message,
				Success = returnResult.IsSuccess,
			});
		}
		[HttpPost("users/{userId}/add-role/{role}")]
		public async Task<ActionResult<ApiResponse>> AddRole(string userId,RolesFields role)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());
			var returnResult = await _claimsService.AddRolesAsync(userId, [role.ToString()]);
			if (!returnResult.IsSuccess)
				return  BadRequest(new ApiResponse
				{
					Errors = returnResult.Errors,
					Message= returnResult.Message,
					Success= returnResult.IsSuccess,
				});
			return BadRequest(new ApiResponse
			{
				Errors = returnResult.Errors,
				Message = returnResult.Message,
				Success = returnResult.IsSuccess,
			});
		}
	}
}

