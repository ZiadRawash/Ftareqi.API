using Ftareqi.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IFcmService
	{
		Task<FcmSendResult> SendNotificationAsync(
			string fcmToken,
			string title,
			string body,
			Dictionary<string, string>? data = null);

		Task<FcmSendResult> SendMultipleNotificationsAsync(
			List<string> fcmTokens,
			string title,
			string body,
			Dictionary<string, string>? data = null);
	}
}
