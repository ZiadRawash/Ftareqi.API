using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.Common.Settings;
using Ftareqi.Application.DTOs.Cloudinary;
using Ftareqi.Application.DTOs.Files;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Ftareqi.Infrastructure.Implementation
{
	public class CloudinaryService : ICloudinaryService
	{
		private readonly CloudinarySettings _cloudinarySettings;
		private readonly Cloudinary _cloudinary;
		private readonly ILogger<CloudinaryService>_logger;
		public CloudinaryService(IOptions<CloudinarySettings> CloudinarySettings , ILogger<CloudinaryService> logger)
		{
			_cloudinarySettings = CloudinarySettings.Value;
			var account = new Account(
			_cloudinarySettings.CloudName,
			_cloudinarySettings.ApiKey,
			_cloudinarySettings.ApiSecret);
			_cloudinary = new Cloudinary(account);
			_logger = logger;
		}

		public async Task<Result<SavedImageDto>> UploadPhoto(CloudinaryReqDto image)
		{
			if (image?.FileStream == null || string.IsNullOrEmpty(image.FileName)){
				_logger.LogWarning("Error With data sent to Cloudinary service");
				return Result<SavedImageDto>.Failure("Invalid input data");
			}
			try
			{
				var uploadParams = new ImageUploadParams
				{
					File = new FileDescription(image.FileName, image.FileStream),
					Folder = "Ftareqi" 
				};

				var uploadResult = await _cloudinary.UploadAsync(uploadParams);

				if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
				{
					var dto = new SavedImageDto
					{
						ImageUrl = uploadResult.SecureUrl.AbsoluteUri,
						deleteId = uploadResult.PublicId
					};
					_logger.LogInformation("{filename} uploaded successfully to Cloudinary", image.FileName);
					return Result<SavedImageDto>.Success(dto);
				}
				_logger.LogWarning("Error With uploading file {filename} to Cloudinary service",image.FileName);
				return Result<SavedImageDto>.Failure("Upload failed");
			}
			catch(Exception ex)  {
				_logger.LogError("exception happened{message}", ex.Message);
				throw;
			}

		}
		public async Task<Result> DeleteImage(string deleteId)
		{
			if (string.IsNullOrEmpty(deleteId))
			{
				_logger.LogWarning("publicId is required");
				return Result.Failure("publicId is required");
			}

			var deletionParams = new DeletionParams(deleteId)
			{
				ResourceType = ResourceType.Image,
				Invalidate = true
			};

			var result = await _cloudinary.DestroyAsync(deletionParams);
			var Destroy = await _cloudinary.DestroyAsync(deletionParams);

			if (result.StatusCode == System.Net.HttpStatusCode.OK && result.Result == "ok")
			{
				return Result.Success("Image deleted successfully");
			}
			_logger.LogWarning("error happened while removing image with publicId {deleteId}", deleteId);
			return Result.Failure("error happened while removing image");
		}
	}
}
