using FirebaseAdmin.Messaging;
using Ftareqi.Application.DTOs;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Ftareqi.Infrastructure.Services
{
	public class FcmService : IFcmService
	{
		private readonly ILogger<FcmService> _logger;

		public FcmService(ILogger<FcmService> logger)
		{
			_logger = logger;
		}

		public async Task<FcmSendResult> SendNotificationAsync(
			string fcmToken,
			string title,
			string body,
			Dictionary<string, string>? data = null)
		{
			var result = new FcmSendResult();

			try
			{
				if (string.IsNullOrEmpty(fcmToken))
				{
					_logger.LogWarning("FCM token is empty");
					result.InvalidTokens.Add(fcmToken);
					return result;
				}

				var message = new Message()
				{
					Token = fcmToken,
					Notification = new Notification()
					{
						Title = title,
						Body = body
					},
					Data = data ?? new Dictionary<string, string>()
				};

				var messaging = FirebaseMessaging.DefaultInstance;

				var response = await messaging.SendAsync(message);

				result.SuccessTokens.Add(fcmToken);

				_logger.LogInformation(
					"FCM notification sent. MessageId: {MessageId}, Token: {Token}",
					response,
					fcmToken);

				return result;
			}
			catch (FirebaseMessagingException ex)
			{
				if (ex.MessagingErrorCode ==
					MessagingErrorCode.Unregistered ||
					ex.MessagingErrorCode ==
					MessagingErrorCode.InvalidArgument)
				{
					result.InvalidTokens.Add(fcmToken);
				}
				else
				{
					result.FailedTokens.Add(fcmToken);
				}

				_logger.LogError(ex,
					"FCM failed for token: {Token}",
					fcmToken);

				return result;
			}
			catch (Exception ex)
			{
				result.FailedTokens.Add(fcmToken);

				_logger.LogError(ex,
					"Unexpected error sending FCM");

				return result;
			}
		}

		public async Task<FcmSendResult> SendMultipleNotificationsAsync(
			List<string> fcmTokens,
			string title,
			string body,
			Dictionary<string, string>? data = null)
		{
			var finalResult = new FcmSendResult();

			try
			{
				if (fcmTokens == null || fcmTokens.Count == 0)
				{
					_logger.LogWarning("No FCM tokens provided");
					return finalResult;
				}

				var messaging = FirebaseMessaging.DefaultInstance;

				var messages = fcmTokens
					.Where(x => !string.IsNullOrEmpty(x))
					.Select(token => new Message
					{
						Token = token,
						Notification = new Notification
						{
							Title = title,
							Body = body
						},
						Data = data ?? new Dictionary<string, string>()
					})
					.ToList();

				var response =
					await messaging.SendEachAsync(messages);

				for (int i = 0; i < response.Responses.Count; i++)
				{
					var token = messages[i].Token;
					var res = response.Responses[i];

					if (res.IsSuccess)
					{
						finalResult.SuccessTokens.Add(token);
					}
					else
					{
						var error = res.Exception;

						if (error is FirebaseMessagingException fcmEx &&
						   (fcmEx.MessagingErrorCode ==
							MessagingErrorCode.Unregistered ||
							fcmEx.MessagingErrorCode ==
							MessagingErrorCode.InvalidArgument))
						{
							finalResult.InvalidTokens.Add(token);
						}
						else
						{
							finalResult.FailedTokens.Add(token);
						}
					}
				}

				_logger.LogInformation(
					"FCM Results → Success: {S}, Invalid: {I}, Failed: {F}",
					finalResult.SuccessTokens.Count,
					finalResult.InvalidTokens.Count,
					finalResult.FailedTokens.Count);

				return finalResult;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,
					"Error sending multiple FCM");

				finalResult.FailedTokens
					.AddRange(fcmTokens);

				return finalResult;
			}
		}
	}
}