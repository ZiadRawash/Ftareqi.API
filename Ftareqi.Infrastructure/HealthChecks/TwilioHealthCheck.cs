using Ftareqi.Application.Common.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.HealthChecks
{
	public class TwilioHealthCheck : IHealthCheck
	{
		private readonly TwilioSettings _twilioSettings;

		public TwilioHealthCheck(IOptions<TwilioSettings> twilioSettings)
		{
			_twilioSettings = twilioSettings.Value;
		}

		public async Task<HealthCheckResult> CheckHealthAsync(
			HealthCheckContext context,
			CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(_twilioSettings.AccountSID) || string.IsNullOrWhiteSpace(_twilioSettings.AuthToken))
			{
				return HealthCheckResult.Unhealthy("Twilio Account SID or Auth Token is not configured.");
			}

			try
			{
				using var httpClient = new HttpClient();
				var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_twilioSettings.AccountSID}:{_twilioSettings.AuthToken}"));
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

				var response = await httpClient.GetAsync(
					$"https://api.twilio.com/2010-04-01/Accounts/{_twilioSettings.AccountSID}.json",
					cancellationToken);

				if (response.StatusCode == HttpStatusCode.OK)
				{
					return HealthCheckResult.Healthy("Twilio account endpoint is reachable and credentials are valid.");
				}

				return HealthCheckResult.Unhealthy($"Twilio account endpoint returned {(int)response.StatusCode} ({response.StatusCode}).");
			}
			catch (Exception ex)
			{
				return HealthCheckResult.Unhealthy("Twilio service is unreachable.", ex);
			}
		}
	}
}