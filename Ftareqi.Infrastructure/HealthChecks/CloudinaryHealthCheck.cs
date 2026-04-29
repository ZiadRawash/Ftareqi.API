using CloudinaryDotNet;
using Ftareqi.Application.Common.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.HealthChecks
{
	public class CloudinaryHealthCheck : IHealthCheck
	{
		private readonly Cloudinary _cloudinary;
		public CloudinaryHealthCheck(IOptions<CloudinarySettings> cloudinarySettings)
		{
			var settings = cloudinarySettings.Value;
			_cloudinary = new Cloudinary(new Account(
				settings.CloudName,
				settings.ApiKey,
				settings.ApiSecret
			));
		}
		public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				var result = await _cloudinary.PingAsync();

				if (result.StatusCode == HttpStatusCode.OK)
				{
					return HealthCheckResult.Healthy("Cloudinary is reachable and credentials are valid.");
				}

				return HealthCheckResult.Unhealthy($"Cloudinary returned an error: {result.StatusCode}");
			}
			catch (Exception ex)
			{
				return HealthCheckResult.Unhealthy("Cloudinary service is unreachable.", ex);
			}
		}
	}
}
