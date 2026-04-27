using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Clients
{
	public interface INotificationClient
	{
		Task ReceiveNotification(NotificationDto notification);

	}
}
