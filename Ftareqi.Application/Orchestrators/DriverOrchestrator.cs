using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Consts;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Mappers;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Http;
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

		public DriverOrchestrator(
			IUserService userService,
			IUnitOfWork unitOfWork,
			IBackgroundJobService backgroundJobService,
			ILogger<DriverOrchestrator> logger,
			IFileMapper fileMapper,
			IUserClaimsService claimsService)
		{
			_unitOfWork = unitOfWork;
			_backgroundJobService = backgroundJobService;
			_logger = logger;
			_fileMapper = fileMapper;
			_claimsService = claimsService;
		}

		//create Driver Profile
		public async Task<Result<DriverProfileResponseDto>> CreateDriverProfileAsync(DriverProfileCreateDto driverDto)
		{

			// Validate user
			var userfound = await _unitOfWork.Users.ExistsAsync(x => x.Id == driverDto.UserId);
			if (userfound == false)
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

		//Create Car For DriverProfile
		public async Task<Result<CarResponseDto>> CreateCarForDriverProfile(CarCreateDto carDto)
		{
			//validate DriverProfile
			var found = await _unitOfWork.DriverProfiles.FirstOrDefaultAsync(x => x.UserId == carDto.UserId);
			if (found == null)
				return Result<CarResponseDto>.Failure("UserProfile is null");
			// create car itself
			var car = new Car
			{
				LicenseExpiryDate=carDto.LicenseExpiryDate,
				DriverProfileId = found!.Id,
				Color = carDto.Color!,
				Model = carDto.Model!,
				Plate = carDto.Palette!,
				NumOfSeats = carDto.NumOfSeats,
				CreatedAt = DateTime.UtcNow,
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
		public async Task<Result<DriverWithCarResponseDto>> GetDriverProfileDetails(int driverProfileId)
		{
			if (driverProfileId < 0)
				return Result<DriverWithCarResponseDto>.Failure("Invalid driver profile id ");
			try
			{
				var profile = await _unitOfWork.DriverProfiles.GetDriverProfilesWithCarAsync(x => x.Status == DriverStatus.Pending&& x.Id== driverProfileId);
				if (profile == null)
				{
					return Result<DriverWithCarResponseDto>.Success(null!);
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
				x => x.User!);

			// Map to DTO
			var results = profilesItems.Select(x => new DriverProfileWithUsernameDto
			{
				DriverProfileId=x.User!.DriverProfile!.Id,
				CreatedAt = x.CreatedAt,
				PhoneNumber = x.User!.PhoneNumber,
				FullName = x.User!.FullName,
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

	}
}
