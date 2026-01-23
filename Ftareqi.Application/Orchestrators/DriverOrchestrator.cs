using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Consts;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.DTOs.Profile;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Mappers;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;



namespace Ftareqi.Application.Orchestrators
{

	public class DriverOrchestrator : IDriverOrchestrator
	{
		private readonly IFileMapper _fileMapper;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ILogger<DriverOrchestrator> _logger;
		private readonly IUserClaimsService _claimsService;
		private readonly ICloudinaryService _cloudinaryService;

		public DriverOrchestrator(
			IUserService userService,
			IUnitOfWork unitOfWork,
			IBackgroundJobService backgroundJobService,
			ILogger<DriverOrchestrator> logger,
			IFileMapper fileMapper,
			IUserClaimsService claimsService,
			ICloudinaryService cloudinaryService)
		{
			_unitOfWork = unitOfWork;
			_backgroundJobService = backgroundJobService;
			_logger = logger;
			_fileMapper = fileMapper;
			_claimsService = claimsService;
			_cloudinaryService = cloudinaryService;
		}

		//create Driver Profile
		public async Task<Result<DriverProfileResponseDto>> CreateDriverProfileAsync(DriverProfileCreateDto driverDto)
		{

			// Validate user
	
			
			var user= await _unitOfWork.Users.FindAllAsNoTrackingAsync(x => x.Id == driverDto.UserId, x=>x.DriverProfile!);
			if (user==null)
			{
				return Result<DriverProfileResponseDto>.Failure("User not found");
			}
			if (user.FirstOrDefault()!.DriverProfile!=null) {
				return Result<DriverProfileResponseDto>.Failure("User already has a driver profile");
			}

			try
			{
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



				var jobId = await _backgroundJobService.EnqueueAsync<IDriverJobs>(
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

		//Create Car For DriverProfile
		public async Task<Result<CarResponseDto>> CreateCarForDriverProfile(CarCreateDto carDto)
		{
			//validate DriverProfile
			var found = await _unitOfWork.DriverProfiles.FindAllAsTrackingAsync(x => x.UserId == carDto.UserId,x=>x.Car!);
			if (!found.Any())
				return Result<CarResponseDto>.Failure("UserProfile don't exist");
			// create car itself
			if (found.FirstOrDefault()!.Car != null)
				return Result<CarResponseDto>.Failure("Driver profile already has a car");
			var driverProfile = found.FirstOrDefault()!;

			var car = new Car
			{
				LicenseExpiryDate=carDto.LicenseExpiryDate,
				DriverProfileId = found.FirstOrDefault()!.Id,
				Color = carDto.Color!,
				Model = carDto.Model!,
				Plate = carDto.Plate!,
				NumOfSeats = carDto.NumOfSeats,
				CreatedAt = DateTime.UtcNow,
			};
			driverProfile.Status = DriverStatus.PendingImageUpload;
			driverProfile.UpdatedAt = DateTime.UtcNow;
			_unitOfWork.DriverProfiles.Update(driverProfile);

			await _unitOfWork.Cars.AddAsync(car);
			await _unitOfWork.SaveChangesAsync();
			_logger.LogInformation("Car{carid} Created successfully to driver profile", car.Id);

			//ConvertPhotos
			var photsList = new List<(IFormFile File, ImageType Type)>
			{
				(carDto.CarPhoto!, ImageType.CarPhoto),
				(carDto.CarLicenseBack!, ImageType.CarLicenseBack),
				(carDto.CarLicenseFront!, ImageType.CarLicenseFront)
			};
			var photosSerialized = _fileMapper.MapFilesWithTypes(photsList);
			//create images 
			var jobId = await _backgroundJobService.EnqueueAsync<ICarJobs>(
				job => job.UploadCarImagesAsync(car.Id, photosSerialized));
			_logger.LogInformation("Background job {jobId} queued for car {carid}", jobId, car.Id);

			//return carDto
			var response = new CarResponseDto
			{
				CarId = car.Id,
				Color = car.Color!,
				CreatedAt = car.CreatedAt,
				Model = car.Model!,
				DriverProfileId = car.DriverProfileId!,
				NumOfSeats = car.NumOfSeats,
				Plate = car.Plate!,
				LicenseExpiryDate= car.LicenseExpiryDate!,
			};
			return Result<CarResponseDto>.Success(response);
		}

		// Get details for driver profile
		public async Task<Result<DriverWithCarResponseDto>> GetDriverDetails(int driverId)
		{
			if (driverId<0)
				return Result<DriverWithCarResponseDto>.Failure("Invalid driver profile id ");
			try
			{
				var profile = await _unitOfWork.DriverProfiles.GetDriverProfilesWithCarAsync(x=> x.Id == driverId);
				if (profile == null)
				{
					return Result<DriverWithCarResponseDto>.Failure("User Not Found");
				}
				if (profile.Status != DriverStatus.Pending)
				{
					return Result<DriverWithCarResponseDto>.Failure("User updating his profile");
				}
				var result = DriverProfileMapper.ToDto(profile);
				return Result<DriverWithCarResponseDto>.Success(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error happened{message}", ex.Message);
				return Result < DriverWithCarResponseDto>.Failure("Unexpected error happened");
			}
		}

		// Get pending profiles for moderator
		public async Task<Result<PaginatedResponse<DriverProfileWithUsernameDto>>> GetPendingDriverProfiles(GenericQueryReq page)
		{
			// Get paged data from repository
			var (profilesItems, totalCount) = await _unitOfWork.DriverProfiles.GetPagedAsync(
				page.Page,
				page.PageSize,
				x => x.CreatedAt,
				x => x.Status == DriverStatus.Pending,
				page.SortDescending,
				x => x.User!,
				x=>x.Images,
				x=>x.Car!);

			// Map to DTO
			var results = profilesItems.Select(x => new DriverProfileWithUsernameDto
			{
				DriverProfileId=x.User!.DriverProfile!.Id,
				CreatedAt = x.CreatedAt,
				PhoneNumber = x.User!.PhoneNumber,
				FullName = x.User!.FullName,
				DriverPhoto = x.Images
			   .Where(img => img.Type == ImageType.DriverProfilePhoto)
			   .Select(img => img.Url)
			   .FirstOrDefault()
			}).ToList();

			// Calculate total pages
			var totalPages = (int)Math.Ceiling((double)totalCount / page.PageSize);

			// Prepare response
			var returnModel = new PaginatedResponse<DriverProfileWithUsernameDto>
			{
				Items = results,
				Page = page.Page,
				PageSize = page.PageSize,
				TotalCount = totalCount,
				TotalPages = totalPages
			};

			return Result<PaginatedResponse<DriverProfileWithUsernameDto>>.Success(returnModel);
		}


		//approve driver profile request
		public async Task <Result> ApproveDriverProfile(int profileId)
		{
		var profileFound= await _unitOfWork.DriverProfiles.GetByIdAsync(profileId);
			if (profileFound == null)
				return Result.Failure("Invalid profile id");
			profileFound.Status=DriverStatus.Active;
			var claimsAdded = await _claimsService.AddClaimAsync(profileFound.UserId, CustomClaimTypes.IsDriver, CustomClaimTypes.True);
			if (claimsAdded.IsFailure)
			{
				return Result.Failure(claimsAdded.Errors);
			}
			profileFound.UpdatedAt = DateTime.UtcNow;
			 _unitOfWork.DriverProfiles.Update(profileFound);
			await _unitOfWork.SaveChangesAsync();
			return Result.Success("Driver Profile approved successfully");
		}

		//reject driver profile request
		public async Task<Result> RejectDriverProfile(int profileId)
		{
			var profileFound = await _unitOfWork.DriverProfiles.GetByIdAsync(profileId);
			if (profileFound == null)
				return Result.Failure("Invalid profile id");

			profileFound.Status = DriverStatus.Rejected;

			var userClaims = await _claimsService.GetUserClaimsAsync(profileFound.UserId);
			if (userClaims.IsSuccess && userClaims.Data != null)
			{
				var driverClaims = userClaims.Data.Where(c => c.Key == CustomClaimTypes.IsDriver);

				foreach (var claim in driverClaims)
				{
					await _claimsService.RemoveClaimAsync(profileFound.UserId, claim.Key);
				}
			}
			profileFound.UpdatedAt = DateTime.UtcNow;
			_unitOfWork.DriverProfiles.Update(profileFound);
			await _unitOfWork.SaveChangesAsync();

			return Result.Success("Driver Profile rejected successfully and driver claims removed.");
		}

		//Update Driver Profile

		public async Task<Result<DriverProfileResponseDto>> UpdateDriverProfileAsync(DriverProfileUpdateDto driverDto)
		{
			try
			{
				// 1. Validate user and get driver profile with images
				var driverProfiles = await _unitOfWork.DriverProfiles.FindAllAsTrackingAsync(
					x => x.UserId == driverDto.UserId,
					x => x.Images!);

				if (driverProfiles == null || !driverProfiles.Any())
				{
					return Result<DriverProfileResponseDto>.Failure("Driver profile not found");
				}

				var driverProfile = driverProfiles.FirstOrDefault()!;

				// 2. Block updates if status is PendingImageUpload
				if (driverProfile.Status == DriverStatus.PendingImageUpload)
				{
					return Result<DriverProfileResponseDto>.Failure(
						"Cannot update profile while images are being uploaded. Please wait for the upload to complete.");
				}

				// 3. Update data fields
				if (driverDto.LicenseExpiryDate.HasValue)
				{
					driverProfile.LicenseExpiryDate = driverDto.LicenseExpiryDate.Value;
				}

				// 4. Handle image updates
				var hasImageUpdates = driverDto.DriverProfilePhoto != null ||
									 driverDto.DriverLicenseFront != null ||
									 driverDto.DriverLicenseBack != null;

				List<string> oldImagePublicIds = new List<string>();
				List<Image> imagesToDelete = new List<Image>();

				if (hasImageUpdates && driverProfile.Images != null && driverProfile.Images.Any())
				{
					// Collect PublicIds and images that will be replaced
					if (driverDto.DriverProfilePhoto != null)
					{
						var oldImage = driverProfile.Images
							.FirstOrDefault(x => x.Type == ImageType.DriverProfilePhoto);
						if (oldImage != null)
						{
							if (!string.IsNullOrWhiteSpace(oldImage.PublicId))
								oldImagePublicIds.Add(oldImage.PublicId);
							imagesToDelete.Add(oldImage);
						}
					}

					if (driverDto.DriverLicenseFront != null)
					{
						var oldImage = driverProfile.Images
							.FirstOrDefault(x => x.Type == ImageType.DriverLicenseFront);
						if (oldImage != null)
						{
							if (!string.IsNullOrWhiteSpace(oldImage.PublicId))
								oldImagePublicIds.Add(oldImage.PublicId);
							imagesToDelete.Add(oldImage);
						}
					}

					if (driverDto.DriverLicenseBack != null)
					{
						var oldImage = driverProfile.Images
							.FirstOrDefault(x => x.Type == ImageType.DriverLicenseBack);
						if (oldImage != null)
						{
							if (!string.IsNullOrWhiteSpace(oldImage.PublicId))
								oldImagePublicIds.Add(oldImage.PublicId);
							imagesToDelete.Add(oldImage);
						}
					}

					// Delete old images from database
					if (imagesToDelete.Any())
					{
						_unitOfWork.Images.DeleteRange(imagesToDelete);

						_logger.LogInformation("Deleted {count} old driver images from database for profile {profileId}",
							imagesToDelete.Count, driverProfile.Id);
					}
				}

				// 5. Set status to Pending and remove claims
				var previousStatus = driverProfile.Status;
				driverProfile.Status = DriverStatus.Pending;
				driverProfile.UpdatedAt = DateTime.UtcNow;

				// Remove driver claims
				if (previousStatus == DriverStatus.Active)
				{
					var userClaims = await _claimsService.GetUserClaimsAsync(driverDto.UserId);
					if (userClaims.IsSuccess && userClaims.Data != null)
					{
						var driverClaims = userClaims.Data.Where(c => c.Key == CustomClaimTypes.IsDriver);
						foreach (var claim in driverClaims)
						{
							await _claimsService.RemoveClaimAsync(driverDto.UserId, claim.Key);
						}
					}
				}

				// 6. Update the profile
				_unitOfWork.DriverProfiles.Update(driverProfile);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Driver profile {profileId} updated. Status changed from {oldStatus} to Pending",
					driverProfile.Id, previousStatus);

				// 7. Handle image deletion and upload via background jobs
				if (hasImageUpdates)
				{
					// Enqueue background job to delete old images from Cloudinary
					if (oldImagePublicIds.Any())
					{
						var deleteJobId = await _backgroundJobService.EnqueueAsync<IDriverJobs>(
							job => job.DeleteDriverImagesAsync(oldImagePublicIds));

						_logger.LogInformation("Background job {jobId} queued for deleting {count} driver images from Cloudinary",
							deleteJobId, oldImagePublicIds.Count);
					}

					// Prepare new images for upload
					var imagesToUpload = new List<(IFormFile File, ImageType Type)>();

					if (driverDto.DriverProfilePhoto != null)
						imagesToUpload.Add((driverDto.DriverProfilePhoto, ImageType.DriverProfilePhoto));

					if (driverDto.DriverLicenseFront != null)
						imagesToUpload.Add((driverDto.DriverLicenseFront, ImageType.DriverLicenseFront));

					if (driverDto.DriverLicenseBack != null)
						imagesToUpload.Add((driverDto.DriverLicenseBack, ImageType.DriverLicenseBack));

					var photosSerialized = _fileMapper.MapFilesWithTypes(imagesToUpload);

					// Enqueue background job for uploading new images
					var uploadJobId = await _backgroundJobService.EnqueueAsync<IDriverJobs>(
						job => job.UploadDriverImagesAsync(
							driverProfile.Id,
							driverDto.UserId,
							photosSerialized));

					_logger.LogInformation("Background job {jobId} queued for uploading new driver images to profile {profileId}",
						uploadJobId, driverProfile.Id);
				}

				var response = new DriverProfileResponseDto
				{
					UserId = driverDto.UserId,
					DriverProfileId = driverProfile.Id,
					Status = driverProfile.Status.ToString(),
					Message = hasImageUpdates
						? "Driver profile updated. Old images are being deleted and new images are being uploaded in the background. Profile set to Pending status for re-approval."
						: "Driver profile updated. Profile set to Pending status for re-approval."
				};

				return Result<DriverProfileResponseDto>.Success(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating driver profile");
				throw;
			}
		}

		//Update car Profile

		public async Task<Result<CarResponseDto>> UpdateCarAsync(CarUpdateDto carDto)
		{
			try
			{
				// 1. Get driver profile with car and images
				var driverProfiles = await _unitOfWork.DriverProfiles.FindAllAsTrackingAsync(
					x => x.UserId == carDto.UserId,
					x => x.Car!,
					x => x.Car!.Images!);

				if (driverProfiles == null || !driverProfiles.Any())
				{
					return Result<CarResponseDto>.Failure("Driver profile not found");
				}

				var driverProfile = driverProfiles.FirstOrDefault()!;

				if (driverProfile.Car == null)
				{
					return Result<CarResponseDto>.Failure("Car not found for this driver profile");
				}

				// 2. Block updates if driver profile status is PendingImageUpload
				if (driverProfile.Status == DriverStatus.PendingImageUpload)
				{
					return Result<CarResponseDto>.Failure(
						"Cannot update car while driver profile images are being uploaded. Please wait for the upload to complete.");
				}

				var car = driverProfile.Car;

				// 3. Update data fields
				if (!string.IsNullOrWhiteSpace(carDto.Color))
					car.Color = carDto.Color;

				if (!string.IsNullOrWhiteSpace(carDto.Model))
					car.Model = carDto.Model;

				if (!string.IsNullOrWhiteSpace(carDto.Plate))
					car.Plate = carDto.Plate;

				if (carDto.NumOfSeats.HasValue)
					car.NumOfSeats = carDto.NumOfSeats.Value;

				if (carDto.LicenseExpiryDate.HasValue)
					car.LicenseExpiryDate = carDto.LicenseExpiryDate.Value;

				// 4. Handle image updates
				var hasImageUpdates = carDto.CarPhoto != null ||
									 carDto.CarLicenseBack != null ||
									 carDto.CarLicenseFront != null;

				List<string> oldImagePublicIds = new List<string>();
				List<Image> imagesToDelete = new List<Image>();

				if (hasImageUpdates && car.Images != null && car.Images.Any())
				{
					// Collect PublicIds and images that will be replaced
					if (carDto.CarPhoto != null)
					{
						var oldImage = car.Images
							.FirstOrDefault(x => x.Type == ImageType.CarPhoto);
						if (oldImage != null)
						{
							if (!string.IsNullOrWhiteSpace(oldImage.PublicId))
								oldImagePublicIds.Add(oldImage.PublicId);
							imagesToDelete.Add(oldImage);
						}
					}

					if (carDto.CarLicenseBack != null)
					{
						var oldImage = car.Images
							.FirstOrDefault(x => x.Type == ImageType.CarLicenseBack);
						if (oldImage != null)
						{
							if (!string.IsNullOrWhiteSpace(oldImage.PublicId))
								oldImagePublicIds.Add(oldImage.PublicId);
							imagesToDelete.Add(oldImage);
						}
					}

					if (carDto.CarLicenseFront != null)
					{
						var oldImage = car.Images
							.FirstOrDefault(x => x.Type == ImageType.CarLicenseFront);
						if (oldImage != null)
						{
							if (!string.IsNullOrWhiteSpace(oldImage.PublicId))
								oldImagePublicIds.Add(oldImage.PublicId);
							imagesToDelete.Add(oldImage);
						}
					}

					// Delete old images from database using DeleteRange
					if (imagesToDelete.Any())
					{
						_unitOfWork.Images.DeleteRange(imagesToDelete);

						_logger.LogInformation("Deleted {count} old car images from database for car {carId}",
							imagesToDelete.Count, car.Id);
					}
				}

				// 5. Set driver profile status to Pending and remove claims
				var previousStatus = driverProfile.Status;
				driverProfile.Status = DriverStatus.Pending;
				driverProfile.UpdatedAt = DateTime.UtcNow;

				// Remove driver claims
				if (previousStatus == DriverStatus.Active)
				{
					var userClaims = await _claimsService.GetUserClaimsAsync(carDto.UserId);
					if (userClaims.IsSuccess && userClaims.Data != null)
					{
						var driverClaims = userClaims.Data.Where(c => c.Key == CustomClaimTypes.IsDriver);
						foreach (var claim in driverClaims)
						{
							await _claimsService.RemoveClaimAsync(carDto.UserId, claim.Key);
						}
					}
				}

				// 6. Update car and driver profile
				car.UpdatedAt = DateTime.UtcNow;
				_unitOfWork.Cars.Update(car);
				_unitOfWork.DriverProfiles.Update(driverProfile);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Car {carId} updated for driver profile {profileId}. Profile status changed from {oldStatus} to Pending",
					car.Id, driverProfile.Id, previousStatus);

				// 7. Handle image deletion and upload via background jobs
				if (hasImageUpdates)
				{
					// Enqueue background job to delete old images from Cloudinary
					if (oldImagePublicIds.Any())
					{
						var deleteJobId = await _backgroundJobService.EnqueueAsync<ICarJobs>(
							job => job.DeleteCarImagesAsync(oldImagePublicIds));

						_logger.LogInformation("Background job {jobId} queued for deleting {count} car images from Cloudinary",
							deleteJobId, oldImagePublicIds.Count);
					}

					// Prepare new images for upload
					var imagesToUpload = new List<(IFormFile File, ImageType Type)>();

					if (carDto.CarPhoto != null)
						imagesToUpload.Add((carDto.CarPhoto, ImageType.CarPhoto));

					if (carDto.CarLicenseBack != null)
						imagesToUpload.Add((carDto.CarLicenseBack, ImageType.CarLicenseBack));

					if (carDto.CarLicenseFront != null)
						imagesToUpload.Add((carDto.CarLicenseFront, ImageType.CarLicenseFront));

					var photosSerialized = _fileMapper.MapFilesWithTypes(imagesToUpload);

					// Enqueue background job for uploading new images
					var uploadJobId = await _backgroundJobService.EnqueueAsync<ICarJobs>(
						job => job.UploadCarImagesAsync(car.Id, photosSerialized));

					_logger.LogInformation("Background job {jobId} queued for uploading new car images to car {carId}",
						uploadJobId, car.Id);
				}

				var response = new CarResponseDto
				{
					CarId = car.Id,
					Color = car.Color!,
					CreatedAt = car.CreatedAt,
					Model = car.Model!,
					DriverProfileId = car.DriverProfileId!,
					NumOfSeats = car.NumOfSeats,
					Plate = car.Plate!,
					LicenseExpiryDate = car.LicenseExpiryDate!,
				};

				return Result<CarResponseDto>.Success(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating car");
				throw;
			}
		}

		public async Task<Result<DriverProfileResponse>> GetDriverProfile(string id)
		{
			if (string.IsNullOrEmpty(id))
				return Result<DriverProfileResponse>.Failure("No such id");
			var driverProfile = await _unitOfWork.DriverProfiles.FirstOrDefaultAsync(x=>x.UserId == id, x=>x.Images);
			if (driverProfile == null)
			{
				return Result<DriverProfileResponse>.Failure("Driver profile doesn't exist");
			}
			var images = driverProfile.Images;

			var response = new DriverProfileResponse
			{
				Id=driverProfile.Id,
				CreatedAt = driverProfile.CreatedAt,
				Status = driverProfile.Status,
				LicenseExpiryDate = driverProfile.LicenseExpiryDate,
				DriverProfilePhoto = images?
					.FirstOrDefault(x => x.Type == ImageType.DriverProfilePhoto)
					?.Url,
				DriverLicenseFront = images?
					.FirstOrDefault(x => x.Type == ImageType.DriverLicenseFront)
					?.Url,
				DriverLicenseBack = images?
					.FirstOrDefault(x => x.Type == ImageType.DriverLicenseBack)
					?.Url
			};
			return Result<DriverProfileResponse>.Success(response);
		}
		public async Task<Result<CarProfileResponseDto>> GetCarByDriverProfileId(int driverProfileId)
		{
			var car = await _unitOfWork.Cars
				.FirstOrDefaultAsync(x => x.DriverProfileId == driverProfileId, x => x.Images);

			if (car == null)
				return Result<CarProfileResponseDto>.Failure("Car not found for this driver");

			var images = car.Images;

			var response = new CarProfileResponseDto
			{
				Id = car.Id,
				Model = car.Model,
				Color = car.Color,
				Plate = car.Plate,
				NumOfSeats = car.NumOfSeats,
				LicenseExpiryDate = car.LicenseExpiryDate,
				CreatedAt = car.CreatedAt,
				CarPhoto = images?
					.FirstOrDefault(x => x.Type == ImageType.CarPhoto)
					?.Url,
				CarLicenseFront = images?
					.FirstOrDefault(x => x.Type == ImageType.CarLicenseFront)
					?.Url,
				CarLicenseBack = images?
					.FirstOrDefault(x => x.Type == ImageType.CarLicenseBack)
					?.Url
			};

			return Result<CarProfileResponseDto>.Success(response);
		}

	}
}
