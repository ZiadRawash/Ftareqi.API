using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.DTOs.User;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Ftareqi.Domain.ValueObjects;
using Ftareqi.Infrastructure.Implementation;
using Ftareqi.Infrastructure.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
	private readonly INotificationOrchestrator _notificationOrchestrator;
	private readonly IUserService _userService;
	private readonly IFcmTokenService _fcmTokenService;

	public NotificationController(
		IFcmTokenService fcmTokenService,
	IUserService userService,
		INotificationOrchestrator notificationOrchestrator)
	{
		_fcmTokenService = fcmTokenService;
		_userService = userService;
		_notificationOrchestrator = notificationOrchestrator;
	}
	[HttpGet]
	public async Task<ActionResult<ApiResponse<PaginatedResponse<NotificationDto>>>> GetAll([FromQuery] GenericQueryReq queryRequest)
	{
		var userId = User.GetUserId();
		if (userId == null)
			return Unauthorized();

		if (!ModelState.IsValid)
		{
			return BadRequest(new ApiResponse
			{
				Errors = ["Invalid parameters"],
				Message = "Invalid Parameter",
				Success = false
			});
		}

		var response = await _notificationOrchestrator.GetAllNotifications(queryRequest, userId);
		if (!response.IsSuccess)
			return BadRequest(new ApiResponse
			{
				Errors = response.Errors,
				Message = response.Message,
				Success = response.IsSuccess
			});

		return Ok(new ApiResponse<PaginatedResponse<NotificationDto>>
		{
			Errors = response.Errors,
			Message = response.Message,
			Success = response.IsSuccess,
			Data = response.Data
		});
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<ApiResponse<NotificationDto>>> GetById(int id)
	{
		var userId = User.GetUserId();
		if (userId == null)
			return Unauthorized();
		var response = await _notificationOrchestrator.GetByIdAsync(id, userId);
		if (!response.IsSuccess)
			return BadRequest(new ApiResponse
			{
				Errors = response.Errors,
				Message = response.Message,
				Success = response.IsSuccess
			});
		return Ok(new ApiResponse<NotificationDto>
		{
			Errors = response.Errors,
			Message = response.Message,
			Success = response.IsSuccess,
			Data = response.Data
		});
	}
	[HttpPut("{id}/mark-as-read")]
	public async Task<ActionResult<ApiResponse>> MarkAsRead(int id)
	{
		var response = await _notificationOrchestrator.MarkAsRead(id);
		if (!response.IsSuccess)
			return BadRequest(new ApiResponse
			{
				Errors = response.Errors,
				Message = response.Message,
				Success = response.IsSuccess
			});

		return Ok(new ApiResponse
		{
			Errors = response.Errors,
			Message = response.Message,
			Success = response.IsSuccess
		});
	}
	[HttpPut("mark-all-as-read")]
	public async Task<ActionResult<ApiResponse>> MarkAllAsRead()
	{
		var userId = User.GetUserId();
		if (userId == null)
			return Unauthorized();

		var response = await _notificationOrchestrator.MarkAllAsReadAsync(userId);
		if (!response.IsSuccess)
			return BadRequest(new ApiResponse
			{
				Errors = response.Errors,
				Message = response.Message,
				Success = response.IsSuccess
			});

		return Ok(new ApiResponse
		{
			Errors = response.Errors,
			Message = response.Message,
			Success = response.IsSuccess
		});
	}
	[HttpGet("unread-count")]
	public async Task<ActionResult<ApiResponse<NotificationCountDto>>> GetUnreadCount()
	{
		var userId = User.GetUserId();
		if (userId == null)
			return Unauthorized();

		var response = await _notificationOrchestrator.GetUnreadCountAsync(userId);
		if (!response.IsSuccess)
			return BadRequest(new ApiResponse
			{
				Errors = response.Errors,
				Message = response.Message,
				Success = response.IsSuccess
			});

		return Ok(new ApiResponse<NotificationCountDto>
		{
			Errors = response.Errors,
			Message = response.Message,
			Success = response.IsSuccess,
			Data = response.Data
		});
	}
	[HttpDelete("{id}")]
	public async Task<ActionResult<ApiResponse>> DeleteNotification(int id)
	{
		var response = await _notificationOrchestrator.DeleteNotification(id);
		if (!response.IsSuccess)
			return BadRequest(new ApiResponse
			{
				Errors = response.Errors,
				Message = response.Message,
				Success = response.IsSuccess
			});

		return Ok(new ApiResponse
		{
			Errors = response.Errors,
			Message = response.Message,
			Success = response.IsSuccess
		});
	}

	[HttpPost("register-fcm-token")]
	public async Task<ActionResult<ApiResponse>>RegisterFcmToken([FromBody] FcmTokenReqDto request)
		{
			var userId = User.GetUserId();
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();
			if (string.IsNullOrWhiteSpace(
				request?.Token))
			{
				return BadRequest(new ApiResponse
					{
						Success = false,
						Message =	
						"FCM token is required"
				});
			}
			var result =await _fcmTokenService.RegisterDeviceAsync(userId,request.Token);
			if (!result.IsSuccess)
			{
				return BadRequest(
					new ApiResponse
					{
						Success = false,
						Message =
						result.Message
				});
			}
			return Ok(new ApiResponse
				{
					Success = true,
					Message =
					"Device registered successfully"
			});
		}
	[HttpPost("deactivate-fcm-token")]
	public async Task<ActionResult<ApiResponse>> DeactivateFcmToken([FromBody] FcmTokenReqDto request)
	{
		var userId = User.GetUserId();
		if (string.IsNullOrEmpty(userId))
			return Unauthorized();
		if (string.IsNullOrWhiteSpace(
			request?.Token))
		{
			return BadRequest(new ApiResponse
			{
				Success = false,
				Message =
					"FCM token is required"
			});
		}
		var result = await _fcmTokenService.DeactivateDeviceAsync(userId, request.Token);
		if (!result.IsSuccess)
		{
			return BadRequest(
				new ApiResponse
				{
					Success = false,
					Message =
					result.Message
				});
		}
		return Ok(new ApiResponse
		{
			Success = true,
			Message =
				"Device Deactivated successfully"
		});
	}
}