using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Mappers;
using Ftareqi.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Orchestrators
{
	public class NotificationOrchestrator : INotificationOrchestrator
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly INotificationBuilder _notificationBuilder;
		private readonly INotificationService _notificationService;
		public NotificationOrchestrator(IUnitOfWork unitOfWork, INotificationBuilder notificationBuilder, INotificationService notificationService)
		{ 
			_unitOfWork = unitOfWork;
			_notificationBuilder = notificationBuilder;
			_notificationService = notificationService;
		}
		public async Task<Result<PaginatedResponse<NotificationDto>>> GetAllNotifications(GenericQueryReq queryRequest, string userId)
		{
			var userFound = await _unitOfWork.Users.FirstOrDefaultAsNoTrackingAsync(x=>x.Id== userId);
			if (userFound == null)
				return Result<PaginatedResponse<NotificationDto>>.Failure("Invalid userId");
			var (paginatedNotifications, TotalCount) = await _unitOfWork.Notifications.GetPagedAsync(queryRequest.Page, queryRequest.PageSize, x => x.CreatedAt, x=>x.UserId== userId, true);
			var response = new PaginatedResponse<NotificationDto>
			{
				Items = paginatedNotifications.Select(NotificationMapper.ToDto).ToList(),
				TotalCount = TotalCount
			};
			return Result<PaginatedResponse<NotificationDto >>.Success(response);
		}

		public async Task<Result<NotificationDto>> GetByIdAsync(int notificationId, string userId)
		{
			var userFound = await _unitOfWork.Users.FirstOrDefaultAsNoTrackingAsync(x => x.Id == userId);
			if (userFound == null)
				return Result<NotificationDto >.Failure("Invalid user id");
			var response = await _unitOfWork.Notifications.FirstOrDefaultAsNoTrackingAsync(x=>x.Id == notificationId);
			if (response == null)
				return Result<NotificationDto>.Failure("Invalid notificationId");
			return Result<NotificationDto>.Success(NotificationMapper.ToDto(response));
		}
		public async Task<Result<NotificationCountDto>> GetUnreadCountAsync(string userId)
		{
			var response  = await _unitOfWork.Notifications.FindAllAsTrackingAsync(x => x.UserId == userId && !x.IsRead);
			var count = new NotificationCountDto
			{
				Count = response.Count()
			};
			return Result<NotificationCountDto>.Success(count);
		}
		public async Task<Result> MarkAllAsReadAsync(string userId)
		{
			var userFound = await _unitOfWork.Users.FirstOrDefaultAsNoTrackingAsync(x => x.Id == userId);
			if (userFound == null)
				return Result.Failure("Invalid userId");
			var notifications = await _unitOfWork.Notifications
				.FindAllAsTrackingAsync(x => x.UserId == userId && !x.IsRead);

			foreach (var notification in notifications)
			{
				notification.IsRead = true;
			}

			_unitOfWork.Notifications.UpdateRange(notifications);
			await _unitOfWork.SaveChangesAsync();

			return Result.Success("Updated successfully");

		}
		public async Task<Result> MarkAsRead(int notificationId)
		{
			var notificationFound = await _unitOfWork.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.IsRead !=true);
			if (notificationFound == null)
				return Result.Failure($"{notificationId} was not found");
			notificationFound.IsRead= true;
			_unitOfWork.Notifications.Update(notificationFound);
			await _unitOfWork.SaveChangesAsync();
			return Result.Success("Updated successfully");

		}
		public async Task<Result> DeleteNotification(int notificationId)
		{
			var notificationFound = await _unitOfWork.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId);
			if (notificationFound == null)
				return Result.Failure($"{notificationId} was not found");
			_unitOfWork.Notifications.Delete(notificationFound);
			await _unitOfWork.SaveChangesAsync();
			return Result.Success("Deleted successfully");

		}

		public async Task<Result> NotifyAsync(NotificationInput notificationInput)
		{
		var notification = _notificationBuilder.CreateNotification(notificationInput);
			await _unitOfWork.Notifications.AddAsync(notification);
			await _unitOfWork.SaveChangesAsync();
			await _notificationService.NotifyUserAsync(notificationInput.UserId, NotificationMapper.ToDto(notification));
			return Result.Success();
		}
	}
}
	