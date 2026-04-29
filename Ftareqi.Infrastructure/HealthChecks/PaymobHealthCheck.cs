using Ftareqi.Application.Common.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.HealthChecks
{
	public class PaymobHealthCheck : IHealthCheck
	{
		private readonly PaymobSettings _paymobSettings;

		public PaymobHealthCheck(IOptions<PaymobSettings> paymobSettings)
		{
			_paymobSettings = paymobSettings.Value;
		}

		public async Task<HealthCheckResult> CheckHealthAsync(
			HealthCheckContext context,
			CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(_paymobSettings.APIKey))
			{
				return HealthCheckResult.Unhealthy("Paymob API key is not configured.");
			}

			try
			{
				using var httpClient = new HttpClient();
				var response = await httpClient.PostAsJsonAsync(
					"https://accept.paymob.com/api/auth/tokens",
					new { api_key = _paymobSettings.APIKey },
					cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					return HealthCheckResult.Unhealthy($"Paymob auth endpoint returned {(int)response.StatusCode} ({response.StatusCode}).");
				}

				var authResponse = await response.Content.ReadFromJsonAsync<PaymobAuthResponse>(cancellationToken: cancellationToken);
				if (authResponse == null || string.IsNullOrWhiteSpace(authResponse.token))
				{
					return HealthCheckResult.Unhealthy("Paymob auth response did not contain a token.");
				}

				return HealthCheckResult.Healthy("Paymob auth endpoint is reachable and credentials are valid.");
			}
			catch (Exception ex)
			{
				return HealthCheckResult.Unhealthy("Paymob service is unreachable.", ex);
			}
		}

		private sealed class PaymobAuthResponse
		{
			public string? token { get; set; }
		}
	}
}