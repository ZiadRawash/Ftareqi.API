using CloudinaryDotNet.Actions;
using Ftareqi.Application.DTOs.Cloudinary;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Models;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.BackgroundJobs
{
	public class CarImageUploadJob : ICarImageUploadJob
	{
		private readonly ICloudinaryService _cloudinaryService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<CarImageUploadJob> _logger;

		public CarImageUploadJob(
			ICloudinaryService cloudinaryService,
			IUnitOfWork unitOfWork,
			ILogger<CarImageUploadJob> logger)
		{
			_cloudinaryService = cloudinaryService;
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
		public async Task UploadCarImages(int carId, List<CloudinaryReqDto> images)
		{
			_logger.LogInformation("Starting image upload for car {CarId}", carId);

			try
			{
				var car = await _unitOfWork.Cars.GetByIdAsync(carId);
				if (car == null)
				{
					_logger.LogError("Car with ID {CarId} not found. Aborting upload.", carId);
					return;
				}

				var cloudinaryResult = await _cloudinaryService.UploadPhotosAsync(images);
				if (cloudinaryResult.IsFailure)
				{
					_logger.LogWarning("Cloudinary upload failed for car {CarId}: {Errors}", carId, cloudinaryResult.Errors);
					throw new Exception($"Cloudinary upload failed: {cloudinaryResult.Errors}");
				}
				var newImages = cloudinaryResult.Data!.Select(x => new Image
				{
					Car = car, 
					CreatedAt = DateTime.UtcNow,
					IsDeleted = false,
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
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Successfully uploaded {Count} images for car {CarId}", newImages.Count, carId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error uploading images for car {CarId}", carId);
				throw; 
			}
		}
	}
}
