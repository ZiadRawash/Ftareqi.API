using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.DTOs.User;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Orchestrators
{
	public interface INotificationOrchestrator
	{
		Task <Result> NotifyAsync (NotificationInput notificationInput);
		Task<Result<PaginatedResponse<NotificationDto>>> GetAllNotifications(GenericQueryReq queryRequest, string userId);
		Task <Result<NotificationDto>> GetByIdAsync(int notificationId, string userId);
		Task<Result> MarkAllAsReadAsync(string userId);
		Task<Result> MarkAsRead(int notificationId); 
		Task<Result<NotificationCountDto>> GetUnreadCountAsync(string userId);
		Task<Result> DeleteNotification(int notificationId);
	}
}
