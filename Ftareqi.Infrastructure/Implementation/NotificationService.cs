using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Models;
using Ftareqi.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class NotificationService : INotificationService
	{
		private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
		public NotificationService(IHubContext<NotificationHub, INotificationClient> hubContext)
		{
			 _hubContext = hubContext;
		}
		public async Task NotifyAllAsync(Notification notification)
		{
			await _hubContext.Clients.All.ReceiveNotification(notification);
		}

		public async Task NotifyGroupAsync(string groupName, Notification notification)
		{
			await _hubContext.Clients.Group(groupName).ReceiveNotification(notification);
		}

		public async Task NotifyUserAsync(string userId, Notification notification)
		{
			await _hubContext.Clients.User(userId).ReceiveNotification(notification);
		}
	}
}
