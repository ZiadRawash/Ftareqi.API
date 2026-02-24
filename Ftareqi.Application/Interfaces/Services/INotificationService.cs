using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface INotificationService
	{
		Task NotifyAllAsync(NotificationDto notification);
		Task NotifyUserAsync(string userId, NotificationDto notification);
		Task NotifyGroupAsync(string groupName, NotificationDto notification);
	}
}
