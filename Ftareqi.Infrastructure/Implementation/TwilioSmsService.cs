using Ftareqi.Application.Common.Results;
using Ftareqi.Application.Common.Settings;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Twilio.Clients; // Added for ITwilioRestClient
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Ftareqi.Infrastructure.Implementation
{
	public class TwilioSmsService : ISmsService
	{
		private readonly TwilioSettings _twilioSettings;
		private readonly ILogger<TwilioSmsService> _logger;
		private readonly ITwilioRestClient _client;

		public TwilioSmsService(
			IOptions<TwilioSettings> options,
			ILogger<TwilioSmsService> logger,
			ITwilioRestClient client)
		{
			_logger = logger;
			_twilioSettings = options.Value;
			_client = client;
		}

		public async Task<Result> SendSMS(string phoneNumber, string otp)
		{
			try
			{
				var messageOptions = new CreateMessageOptions(new PhoneNumber(phoneNumber))
				{
					From = new PhoneNumber(_twilioSettings.TwilioPhoneNumber),
					Body = $"Your Ftareqi OTP is: {otp}. It expires in 10 minutes."
				};

				var message = await MessageResource.CreateAsync(messageOptions, _client);

				bool isSuccessful = message.ErrorCode == null &&
									(message.Status == MessageResource.StatusEnum.Sent ||
									 message.Status == MessageResource.StatusEnum.Queued ||
									 message.Status == MessageResource.StatusEnum.Accepted);
				if (isSuccessful)
				{
					_logger.LogInformation("SMS sent successfully to {PhoneNumber}. SID: {SID}", phoneNumber, message.Sid);
					return Result.Success();
				}

				_logger.LogWarning("SMS failed to {PhoneNumber}. Status: {Status}, Error: {Error}",
					phoneNumber, message.Status, message.ErrorMessage);

				return Result.Failure($"Message failed with status: {message.Status}. Error: {message.ErrorMessage}");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An unexpected error occurred while sending SMS to {PhoneNumber}", phoneNumber);

				return Result.Failure("An internal error occurred while processing the SMS request.");
			}
		}
	}
}