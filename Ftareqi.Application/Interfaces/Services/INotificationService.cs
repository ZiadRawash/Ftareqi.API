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
		Task NotifyAllAsync(Notification notification);
		Task NotifyUserAsync(string userId, Notification notification);
		Task NotifyGroupAsync(string groupName, Notification notification);
	}
}
