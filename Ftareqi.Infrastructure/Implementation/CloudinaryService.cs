using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.Common.Settings;
using Ftareqi.Application.DTOs.Cloudinary;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class CloudinaryService : ICloudinaryService
	{
		private readonly Cloudinary _cloudinary;
		private readonly ILogger<CloudinaryService> _logger;

		public CloudinaryService(IOptions<CloudinarySettings> cloudinarySettings, ILogger<CloudinaryService> logger)
		{
			var settings = cloudinarySettings.Value;
			_cloudinary = new Cloudinary(new Account(
				settings.CloudName,
				settings.ApiKey,
				settings.ApiSecret
			));
			_logger = logger;
		}

		public async Task<Result<SavedImageDto>> UploadPhotoAsync(CloudinaryReqDto image)
		{
			if (!IsValidImage(image))
			{
				_logger.LogWarning("Invalid input for single image upload");
				return Result<SavedImageDto>.Failure("Invalid input data");
			}

			try
			{
				var uploadResult = await _cloudinary.UploadAsync(new ImageUploadParams
				{
					File = new FileDescription(image.FileName, image.FileStream),
					Folder = "Ftareqi"
				});

				if (uploadResult.StatusCode != HttpStatusCode.OK)
				{
					_logger.LogWarning("Upload failed for {file}", image.FileName);
					return Result<SavedImageDto>.Failure("Upload failed");
				}

				var dto = new SavedImageDto
				{
					ImageUrl = uploadResult.SecureUrl.AbsoluteUri,
					deleteId = uploadResult.PublicId,
					ImageType = image.imageType
				};

				_logger.LogInformation("{file} uploaded successfully", image.FileName);
				return Result<SavedImageDto>.Success(dto);
			}
			catch (System.Exception ex)
			{
				_logger.LogError(ex, "Exception during single image upload");
				throw;
			}
		}

		public async Task<Result<List<SavedImageDto>>> UploadPhotosAsync(List<CloudinaryReqDto> images)
		{
			if (images == null || images.Count == 0)
				return Result<List<SavedImageDto>>.Failure("No images provided");

			// Validate all images before uploading
			var invalidImages = images.Where(img => !IsValidImage(img)).ToList();
			if (invalidImages.Any())
			{
				_logger.LogWarning("{count} invalid images provided", invalidImages.Count);
				return Result<List<SavedImageDto>>.Failure($"{invalidImages.Count} invalid images provided");
			}

			try
			{
				// Upload all images in parallel
				var uploadTasks = images.Select(UploadPhotoAsync).ToList();
				var results = await Task.WhenAll(uploadTasks);

				// Check if any failed AFTER all uploads complete
				var failures = results.Where(r => r.IsFailure).ToList();

				if (failures.Any())
				{
					// Get all successful uploads for rollback
					var successful = results.Where(r => r.IsSuccess).Select(r => r.Data!).ToList();

					_logger.LogWarning("{failCount} of {total} uploads failed. Rolling back {successCount} successful uploads",
						failures.Count, images.Count, successful.Count);

					await RollbackUploads(successful);
					return Result<List<SavedImageDto>>.Failure(failures.First().Errors);
				}

				var uploadedImages = results.Select(r => r.Data!).ToList();
				_logger.LogInformation("Batch upload completed. {count} images uploaded", uploadedImages.Count);
				return Result<List<SavedImageDto>>.Success(uploadedImages);
			}
			catch (System.Exception ex)
			{
				_logger.LogError(ex, "Exception during batch upload");
				throw;
			}
		}

		public async Task<Result> DeleteImageAsync(string deleteId)
		{
			if (string.IsNullOrWhiteSpace(deleteId))
				return Result.Failure("publicId is required");

			try
			{
				var deletionParams = new DeletionParams(deleteId)
				{
					ResourceType = ResourceType.Image,
					Invalidate = true
				};

				var result = await _cloudinary.DestroyAsync(deletionParams);

				if (result.StatusCode == HttpStatusCode.OK && result.Result == "ok")
				{
					_logger.LogInformation("Image deleted successfully: {id}", deleteId);
					return Result.Success("Image deleted successfully");
				}

				_logger.LogWarning("Failed to delete image {id}. Status: {status}, Result: {result}",
					deleteId, result.StatusCode, result.Result);
				return Result.Failure("Error deleting image");
			}
			catch (System.Exception ex)
			{
				_logger.LogError(ex, "Exception while deleting image {id}", deleteId);
				return Result.Failure($"Exception during deletion: {ex.Message}");
			}
		}

		public async Task<Result> DeleteImagesAsync(List<string> deleteIds)
		{
			if (deleteIds == null || deleteIds.Count == 0)
				return Result.Failure("No delete IDs provided");

			var validIds = deleteIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();

			if (validIds.Count == 0)
				return Result.Failure("No valid delete IDs provided");

			var deleteTasks = validIds.Select(DeleteImageAsync).ToList();
			var results = await Task.WhenAll(deleteTasks);

			var failureCount = results.Count(r => r.IsFailure);

			if (failureCount > 0)
			{
				_logger.LogWarning("{failCount} of {total} deletions failed", failureCount, validIds.Count);
			}
			else
			{
				_logger.LogInformation("Successfully deleted all {count} images", validIds.Count);
			}

			return Result.Success($"Deletion completed: {validIds.Count - failureCount}/{validIds.Count} succeeded");
		}

		private async Task RollbackUploads(List<SavedImageDto> uploadedImages)
		{
			if (uploadedImages == null || uploadedImages.Count == 0)
				return;

			_logger.LogWarning("Rolling back {count} images", uploadedImages.Count);

			var rollbackTasks = uploadedImages
				.Where(img => !string.IsNullOrWhiteSpace(img.deleteId))
				.Select(img => DeleteImageAsync(img.deleteId!));

			await Task.WhenAll(rollbackTasks);
		}

		private bool IsValidImage(CloudinaryReqDto img)
		{
			return img != null &&
				   img.FileStream != null &&
				   !string.IsNullOrWhiteSpace(img.FileName);
		}
	}
}