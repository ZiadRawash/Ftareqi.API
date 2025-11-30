// Ftareqi.Application/Orchestrators/DriverOrchestrator.cs
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.BackgroundJobs;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ftareqi.Application.Orchestrators
{
	/// <summary>
	/// Orchestrator in Application Layer
	/// Depends only on INTERFACES, not concrete implementations
	/// </summary>
	public class DriverOrchestrator : IDriverOrchestrator
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserService _userService;
		private readonly IBackgroundJobService _backgroundJobService; // Generic abstraction
		private readonly ILogger<DriverOrchestrator> _logger;

		public DriverOrchestrator(
			IUnitOfWork unitOfWork,
			IUserService userService,
			IBackgroundJobService backgroundJobService, // Injected abstraction
			ILogger<DriverOrchestrator> logger)
		{
			_unitOfWork = unitOfWork;
			_userService = userService;
			_backgroundJobService = backgroundJobService;
			_logger = logger;
		}

		public async Task<Result<DriverProfileResponseDto>> CreateDriverProfileAsync(DriverProfileReqDto driverDto)
		{
			_logger.LogInformation("Starting driver profile creation for phone: {phone}", driverDto.PhoneNumber);

			// Validate user
			var userResult = await _userService.GetUserByPhoneAsync(driverDto.PhoneNumber);
			if (userResult.IsFailure || userResult.Data == null)
			{
				return Result<DriverProfileResponseDto>.Failure(userResult.Errors);
			}

			// Validate files
			if (!ValidateFiles(driverDto))
			{
				return Result<DriverProfileResponseDto>.Failure("All image files are required");
			}

			try
			{
				// Create driver profile
				var driverProfile = new DriverProfile
				{
					UserId = userResult.Data.Id,
					CreatedAt = DateTime.UtcNow,
					LicenseExpiryDate = driverDto.LicenseExpiryDate,
					Status = DriverStatus.PendingImageUpload
				};

				await _unitOfWork.DriverProfiles.AddAsync(driverProfile);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Driver profile {profileId} created", driverProfile.Id);

				// Convert files to byte arrays
				var imagesToUpload = new List<ImageUploadData>
				{
					await ConvertToImageUploadData(driverDto.DriverProfilePhoto!, ImageType.DriverProfilePhoto),
					await ConvertToImageUploadData(driverDto.DriverLicenseFront!, ImageType.DriverLicenseFront),
					await ConvertToImageUploadData(driverDto.DriverLicenseBack!, ImageType.DriverLicenseBack)
				};

				// Enqueue job using generic interface - NO HANGFIRE DEPENDENCY HERE!
				var jobId = await _backgroundJobService.EnqueueAsync<IDriverImageUploadJob>(
					job => job.UploadDriverImagesAsync(
						driverProfile.Id,
						userResult.Data.Id,
						imagesToUpload));

				_logger.LogInformation("Background job {jobId} queued for profile {profileId}",
					jobId, driverProfile.Id);

				var response = new DriverProfileResponseDto
				{
					UserId = userResult.Data.Id,
					DriverProfileId = driverProfile.Id,
					Status = driverProfile.Status.ToString(),
					Message = "Driver profile created. Images are being uploaded in the background."
				};

				return Result<DriverProfileResponseDto>.Success(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating driver profile");
				throw;
			}
		}

		private async Task<ImageUploadData> ConvertToImageUploadData(IFormFile file, ImageType imageType)
		{
			using var memoryStream = new MemoryStream();
			await file.CopyToAsync(memoryStream);

			return new ImageUploadData
			{
				FileName = file.FileName,
				FileBytes = memoryStream.ToArray(),
				ImageType = imageType
			};
		}

		private bool ValidateFiles(DriverProfileReqDto dto)
		{
			return dto.DriverProfilePhoto != null && dto.DriverProfilePhoto.Length > 0 &&
				   dto.DriverLicenseFront != null && dto.DriverLicenseFront.Length > 0 &&
				   dto.DriverLicenseBack != null && dto.DriverLicenseBack.Length > 0;
		}
	}
}