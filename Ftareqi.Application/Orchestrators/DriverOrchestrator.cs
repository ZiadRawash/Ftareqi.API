using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.BackgroundJobs;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Hangfire.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Ftareqi.Application.Orchestrators
{
	/// <summary>
	/// Orchestrator in Application Layer
	/// Depends only on INTERFACES, not concrete implementations
	/// </summary>
	public class DriverOrchestrator : IDriverOrchestrator
	{
		private readonly IFileMapper _fileMapper;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IBackgroundJobService _backgroundJobService; 
		private readonly ILogger<DriverOrchestrator> _logger;

		public DriverOrchestrator(
			IUserService userService,
			IUnitOfWork unitOfWork,
			IBackgroundJobService backgroundJobService, 
			ILogger<DriverOrchestrator> logger,
			IFileMapper fileMapper)
		{
			_unitOfWork = unitOfWork;
			_backgroundJobService = backgroundJobService;
			_logger = logger;
			_fileMapper = fileMapper;
		}

		public async Task<Result<DriverProfileResponseDto>> CreateDriverProfileAsync(DriverProfileCreateDto driverDto)
		{

			// Validate user
			var userfound = await _unitOfWork.Users.ExistsAsync(x => x.Id == driverDto.UserId);
			if (userfound == false )
			{
				return Result<DriverProfileResponseDto>.Failure("User not found");
			}
			try
			{
				// Create driver profile
				var driverProfile = new DriverProfile
				{
					UserId = driverDto.UserId,
					CreatedAt = DateTime.UtcNow,
					LicenseExpiryDate = driverDto.LicenseExpiryDate,
					Status = DriverStatus.PendingImageUpload
				};

				await _unitOfWork.DriverProfiles.AddAsync(driverProfile);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Driver profile {profileId} created", driverProfile.Id);

				var imagesToUpload = new List<(IFormFile File, ImageType Type)>
				{
					(driverDto.DriverProfilePhoto!, ImageType.DriverProfilePhoto),
					(driverDto.DriverLicenseFront!, ImageType.DriverLicenseFront),
					(driverDto.DriverLicenseBack!, ImageType.DriverLicenseBack)
				};
				var photosSerialized = _fileMapper.MapFilesWithTypes(imagesToUpload);


				// Enqueue job using generic interface - NO HANGFIRE DEPENDENCY HERE!
				var jobId = await _backgroundJobService.EnqueueAsync<IDriverImageUploadJob>(
					job => job.UploadDriverImagesAsync(
						driverProfile.Id,
						driverDto.UserId,
						photosSerialized));

				_logger.LogInformation("Background job {jobId} queued for profile {profileId}",
					jobId, driverProfile.Id);

				var response = new DriverProfileResponseDto
				{
					UserId = driverDto.UserId,
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

		public async Task<Result<CarResponseDto>> CreateCarForDriverProfile(CarCreateDto carDto)
		{
			//validate DriverProfile
			var found = await _unitOfWork.DriverProfiles.FirstOrDefaultAsync(x=>x.UserId==carDto.UserId);
			if (found == null )
				return Result<CarResponseDto>.Failure("UserProfile is null");
			// create car itself
			var car = new Car
			{
				DriverProfileId = found!.Id,
				Color = carDto.Color!,
				Model=carDto.Model!,
				palette = carDto.Palette!,
				NumOfSeats = carDto.NumOfSeats,
				CreatedAt= DateTime.UtcNow,				
			};
			await _unitOfWork.Cars.AddAsync(car);
			await _unitOfWork.SaveChangesAsync();
			_logger.LogInformation("Car{carid} Created successfully to driver profile{driver} ", car.Id, car.DriverProfile!.Id);

			//ConvertPhotos
			//-----------------------------------------------------------
			var photsList = new List<(IFormFile File, ImageType Type)>
			{
				(carDto.CarPhoto!, ImageType.CarPhoto),
				(carDto.CarLicenseBack!, ImageType.CarLicenseBack),
				(carDto.CarLicenseFront!, ImageType.CarLicenseFront)
			};
			var photosSerialized = _fileMapper.MapFilesWithTypes(photsList);
			//create images 
			var jobId = await _backgroundJobService.EnqueueAsync<ICarImageUploadJob>(
				job => job.UploadCarImages(car.Id, photosSerialized));
			_logger.LogInformation("Background job {jobId} queued for car {carid}",jobId, car.Id);

			//return carDto
			var response = new CarResponseDto
			{
				CarId = car.Id,
				Color = car.Color!,
				CreatedAt = car.CreatedAt,
				Model = car.Model!,
				DriverProfileId = car.DriverProfileId!,
				NumOfSeats = car.NumOfSeats,
				palette = car.palette!,
			};
			return Result<CarResponseDto>.Success(response);
		}
	}
}