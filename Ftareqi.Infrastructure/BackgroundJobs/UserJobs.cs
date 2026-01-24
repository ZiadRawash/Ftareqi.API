using CloudinaryDotNet;
using Ftareqi.Application.DTOs.Cloudinary;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;

namespace Ftareqi.Infrastructure.BackgroundJobs
{
	public class UserJobs : IUserJobs
	{
		private readonly ICloudinaryService  _cloudinary;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<UserJobs> _logger;
		public UserJobs(ICloudinaryService cloudinary , IUnitOfWork unitOfWork , ILogger<UserJobs> logger)
		{
			_cloudinary = cloudinary;
			_unitOfWork = unitOfWork;
			_logger = logger;
			
		}
		[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
		public async Task UploadProfileImage(CloudinaryReqDto image, string userId)
		{
			var uploaded = await _cloudinary.UploadPhotoAsync(image);
			if (uploaded.IsFailure)
			{
				_logger.LogError("Cloudinary upload failed for user profile image {userId}: {Errors}", userId, uploaded.Errors);
				throw new Exception($"Cloudinary upload failed: {uploaded.Errors}");
			}
			var imageToUpload = new Image
			{
				CreatedAt = DateTime.UtcNow,
				UserId = userId,
				PublicId = uploaded.Data!.deleteId!,
				Url = uploaded.Data!.ImageUrl!,
				Type = ImageType.UserProfile,


			};
			await _unitOfWork.Images.AddAsync(imageToUpload);
			await _unitOfWork.SaveChangesAsync();
		}
		[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
		public async Task DeleteProfileImage(string publicId)
		{
			_logger.LogInformation("Starting profile image deletion for publicId {PublicId}", publicId);

			var result = await _cloudinary.DeleteImageAsync(publicId);

			if (result.IsFailure)
			{
				_logger.LogError("Failed to delete profile image from Cloudinary: {Message}", result.Message);
				throw new Exception($"Failed to delete profile image: {result.Message}");
			}

			_logger.LogInformation("Successfully deleted profile image with publicId {PublicId}", publicId);
		}
	}
}
