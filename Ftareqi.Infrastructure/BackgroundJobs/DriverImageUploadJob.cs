using Ftareqi.Application.DTOs.BackgroundJobs;
using Ftareqi.Application.DTOs.Cloudinary;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Hangfire;
using Microsoft.Extensions.Logging;

public class DriverImageUploadJob : IDriverImageUploadJob
{
	private readonly ICloudinaryService _cloudinaryService;
	private readonly IUnitOfWork _unitOfWork;
	private readonly ILogger<DriverImageUploadJob> _logger;

	public DriverImageUploadJob(
		ICloudinaryService cloudinaryService,
		IUnitOfWork unitOfWork,
		ILogger<DriverImageUploadJob> logger)
	{
		_cloudinaryService = cloudinaryService;
		_unitOfWork = unitOfWork;
		_logger = logger;
	}

	[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
	public async Task UploadDriverImagesAsync(
		int driverProfileId,
		string userId,
		List<ImageUploadData> imagesToUpload)
	{
		_logger.LogInformation("Starting image upload for driver profile {profileId}", driverProfileId);

		try
		{
		
			var cloudinaryRequests = imagesToUpload.Select(img => new CloudinaryReqDto
			{
				FileName = img.FileName,
				FileStream = new MemoryStream(img.FileBytes),
				imageType = img.ImageType
			}).ToList();

		
			var uploadResult = await _cloudinaryService.UploadPhotosAsync(cloudinaryRequests);

			if (uploadResult.IsFailure)
			{
				_logger.LogError("Image upload failed for profile {profileId}: {errors}",
					driverProfileId,
					string.Join(", ", uploadResult.Errors));

				await UpdateStatus(driverProfileId, DriverStatus.ImageUploadFailed);
				throw new Exception($"Cloudinary upload failed: {uploadResult.Errors}");
			}

			var imageEntities = uploadResult.Data!.Select(img => new Image
			{
				Url = img.ImageUrl!,
				PublicId = img.deleteId!,
				Type = img.ImageType,
				IsDeleted = false,
				CreatedAt = DateTime.UtcNow,
				DriverProfileId = driverProfileId
			}).ToList();

			await _unitOfWork.Images.AddRangeAsync(imageEntities);

			await UpdateStatusInternal(
				driverProfileId,
				DriverStatus.Pending, 
				commit: false);

			await _unitOfWork.SaveChangesAsync();

			_logger.LogInformation(
				"Successfully uploaded {count} images for driver profile {profileId}",
				imageEntities.Count,
				driverProfileId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error uploading images for profile {profileId}", driverProfileId);

			await UpdateStatusInternal(
				driverProfileId,
				DriverStatus.ImageUploadFailed,
				commit: true);

			throw;
		}
	}

	private async Task UpdateStatusInternal(int driverProfileId, DriverStatus status, bool commit)
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
			await _unitOfWork.SaveChangesAsync();
	}
	private Task UpdateStatus(int driverProfileId, DriverStatus status)
		=> UpdateStatusInternal(driverProfileId, status, commit: true);
}
