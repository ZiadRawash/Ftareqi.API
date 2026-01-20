using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.BackgroundJobs
{
	public class DriverImageDeleteJob : IDriverImageDeleteJob
	{
		private readonly ICloudinaryService _cloudinaryService;
		private readonly ILogger<DriverImageDeleteJob> _logger;

		public DriverImageDeleteJob(
			ICloudinaryService cloudinaryService,
			ILogger<DriverImageDeleteJob> logger)
		{
			_cloudinaryService = cloudinaryService;
			_logger = logger;
		}

		public async Task DeleteDriverImagesAsync(List<string> publicIds)
		{
			_logger.LogInformation("Starting deletion of {count} driver images from Cloudinary", publicIds.Count);

			var result = await _cloudinaryService.DeleteImagesAsync(publicIds);

			if (result.IsFailure)
			{
				_logger.LogError("Failed to delete driver images from Cloudinary: {message}", result.Message);
				throw new Exception($"Failed to delete driver images: {result.Message}");
			}

			_logger.LogInformation("Successfully deleted driver images from Cloudinary: {message}", result.Message);
		}
	}
}
