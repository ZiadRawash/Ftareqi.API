using Ftareqi.Application.Common.Consts;
using Ftareqi.Application.DTOs.BackgroundJobs;
using Ftareqi.Application.DTOs.Cloudinary;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.BackgroundJobs
{
	public class DriverJobs : IDriverJobs
	{
		private readonly ICloudinaryService _cloudinaryService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserClaimsService _userClaimsService;
		private readonly ILogger<DriverJobs> _logger;

		public DriverJobs(
			ICloudinaryService cloudinaryService,
			IUnitOfWork unitOfWork,
			IUserClaimsService userClaimsService,
			ILogger<DriverJobs> logger)
		{
			_cloudinaryService = cloudinaryService;
			_unitOfWork = unitOfWork;
			_userClaimsService = userClaimsService;
			_logger = logger;
		}

		// Delete driver images from Cloudinary
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

		// Upload driver images with automatic retry
		[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
		public async Task UploadDriverImagesAsync(
			int driverProfileId,
			string userId,
			List<CloudinaryReqDto> imagesToUpload)
		{
			_logger.LogInformation("Starting image upload for driver profile {profileId}", driverProfileId);

			try
			{
				var uploadResult = await _cloudinaryService.UploadPhotosAsync(imagesToUpload);

				if (uploadResult.IsFailure)
				{
					_logger.LogError("Image upload failed for profile {profileId}: {errors}",
						driverProfileId, string.Join(", ", uploadResult.Errors));
					await UpdateDriverStatusAsync(driverProfileId, DriverStatus.ImageUploadFailed);
					throw new Exception($"Cloudinary upload failed: {uploadResult.Errors}");
				}

				var imageEntities = uploadResult.Data!.Select(img => new Image
				{
					Url = img.ImageUrl!,
					PublicId = img.deleteId!,
					Type = img.ImageType,
					CreatedAt = DateTime.UtcNow,
					DriverProfileId = driverProfileId
				}).ToList();

				await _unitOfWork.Images.AddRangeAsync(imageEntities);
				await UpdateDriverStatusAsync(driverProfileId, DriverStatus.Pending, commit: false);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation(
					"Successfully uploaded {count} images for driver profile {profileId}",
					imageEntities.Count,
					driverProfileId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error uploading images for profile {profileId}", driverProfileId);
				await UpdateDriverStatusAsync(driverProfileId, DriverStatus.ImageUploadFailed, commit: true);
				throw;
			}
		}

		// Deactivate drivers with expired licenses
		public async Task DeactivateExpiredDriversAsync()
		{
			var expirationDate = DateTime.UtcNow.AddDays(1);

			var expiredProfiles = await _unitOfWork.DriverProfiles.FindAllAsTrackingAsync(
				dp => dp.LicenseExpiryDate <= expirationDate
				   || (dp.Car != null && dp.Car.LicenseExpiryDate <= expirationDate),
				x => x.Car!
			);

			foreach (var profile in expiredProfiles)
			{
				profile.Status = DriverStatus.Expired;
				await _userClaimsService.RemoveClaimAsync(profile.UserId, CustomClaimTypes.IsDriver);
			}

			_unitOfWork.DriverProfiles.UpdateRange(expiredProfiles);
			await _unitOfWork.SaveChangesAsync();

			_logger.LogInformation("Deactivated {Count} expired driver profiles", expiredProfiles.Count());
		}

		// Helper method to update driver profile status
		private async Task UpdateDriverStatusAsync(int driverProfileId, DriverStatus status, bool commit = true)
		{
			var profile = await _unitOfWork.DriverProfiles.GetByIdAsync(driverProfileId);

			if (profile == null)
			{
				_logger.LogWarning("Profile {profileId} not found while updating status", driverProfileId);
				return;
			}

			profile.Status = status;
			profile.UpdatedAt = DateTime.UtcNow;
			_unitOfWork.DriverProfiles.Update(profile);

			if (commit)
			{
				await _unitOfWork.SaveChangesAsync();
			}
		}
	}
}
