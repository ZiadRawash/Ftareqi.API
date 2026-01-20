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
	public class CarImageDeleteJob : ICarImageDeleteJob
	{
		private readonly ICloudinaryService _cloudinaryService;
		private readonly ILogger<CarImageDeleteJob> _logger;

		public CarImageDeleteJob(
			ICloudinaryService cloudinaryService,
			ILogger<CarImageDeleteJob> logger)
		{
			_cloudinaryService = cloudinaryService;
			_logger = logger;
		}

		public async Task DeleteCarImagesAsync(List<string> publicIds)
		{
			_logger.LogInformation("Starting deletion of {count} car images from Cloudinary", publicIds.Count);

			var result = await _cloudinaryService.DeleteImagesAsync(publicIds);

			if (result.IsFailure)
			{
				_logger.LogError("Failed to delete car images from Cloudinary: {message}", result.Message);
				throw new Exception($"Failed to delete car images: {result.Message}");
			}

			_logger.LogInformation("Successfully deleted car images from Cloudinary: {message}", result.Message);
		}
	}
}
