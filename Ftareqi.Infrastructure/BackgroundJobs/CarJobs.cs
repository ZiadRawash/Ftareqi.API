using Ftareqi.Application.DTOs.Cloudinary;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ftareqi.Domain.Models;

namespace Ftareqi.Infrastructure.BackgroundJobs
{
	public class CarJobs : ICarJobs
	{
		private readonly ICloudinaryService _cloudinaryService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<CarJobs> _logger;

		public CarJobs(
			ICloudinaryService cloudinaryService,
			IUnitOfWork unitOfWork,
			ILogger<CarJobs> logger)
		{
			_cloudinaryService = cloudinaryService;
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		// Delete car images from Cloudinary
		public async Task DeleteCarImagesAsync(List<string> publicIds)
		{
			_logger.LogInformation("Starting deletion of {Count} car images from Cloudinary", publicIds.Count());

			var result = await _cloudinaryService.DeleteImagesAsync(publicIds);

			if (result.IsFailure)
			{
				_logger.LogError("Failed to delete car images from Cloudinary: {Message}", result.Message);
				throw new Exception($"Failed to delete car images: {result.Message}");
			}

			_logger.LogInformation("Successfully deleted car images from Cloudinary: {Message}", result.Message);
		}

		// Upload car images with automatic retry
		[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
		public async Task UploadCarImagesAsync(int carId, List<CloudinaryReqDto> images)
		{
			_logger.LogInformation("Starting image upload for car {CarId}", carId);

			try
			{
				var cars = await _unitOfWork.Cars.FindAllAsTrackingAsync(
					x => x.Id == carId,
					x => x.DriverProfile!);

				var car = cars.FirstOrDefault();

				if (car == null)
				{
					_logger.LogError("Car with ID {CarId} not found. Aborting upload.", carId);
					return;
				}

				var cloudinaryResult = await _cloudinaryService.UploadPhotosAsync(images);

				if (cloudinaryResult.IsFailure)
				{
					_logger.LogError("Cloudinary upload failed for car {CarId}: {Errors}",
						carId, cloudinaryResult.Errors);
					throw new Exception($"Cloudinary upload failed: {cloudinaryResult.Errors}");
				}

				var newImages = cloudinaryResult.Data!.Select(x => new Image
				{
					Car = car,
					CreatedAt = DateTime.UtcNow,
					PublicId = x.deleteId!,
					Type = x.ImageType,
					Url = x.ImageUrl!
				}).ToList();

				if (!newImages.Any())
				{
					_logger.LogWarning("No images to upload for car {CarId}", carId);
					return;
				}

				await _unitOfWork.Images.AddRangeAsync(newImages);

				// Update driver profile status if needed
				if (car.DriverProfile != null && car.DriverProfile.Status == DriverStatus.PendingImageUpload)
				{
					car.DriverProfile.Status = DriverStatus.Pending;
					car.DriverProfile.UpdatedAt = DateTime.UtcNow;
				}

				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation(
					"Successfully uploaded {Count} images for car {CarId}",
					newImages.Count(),
					carId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error uploading images for car {CarId}", carId);
				throw;
			}
		}
	}
}
