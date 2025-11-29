using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.DTOs.Files;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class DriverOrchestrator : IDriverOrchestrator
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserService _userService;
		private readonly ICloudinaryService _cloudinaryService;
		private readonly IFileMapper _fileMapper;
		public DriverOrchestrator(IUnitOfWork unitOfWork,
			IUserService userService,
			ICloudinaryService cloudinaryService,
			IFileMapper fileMapper)
		{
		_unitOfWork = unitOfWork;	
		_userService = userService;
		_cloudinaryService = cloudinaryService ;
		_fileMapper = fileMapper ;
		}
		public async Task<Result<string?>> CreateDriverProfile(DriverProfileReqDto driverDto)
		{
			var userFound = await _userService.GetUserByPhoneAsync(driverDto.PhoneNumber);
			if (userFound.IsFailure && userFound.Data!.Id==null)
				return Result<string?>.Failure(userFound.Errors);

			var images = _fileMapper.MapFiles([driverDto.DriverProfilePhoto, driverDto.DriverLicenseFront, driverDto.DriverLicenseBack]);

			if (images == null || images.Count != 3)
			{
				return Result<string?>.Failure("Invalid file data provided");
			}

			var cloudinaryReqDtoList = new List<CloudinaryReqDto>
			{
				new CloudinaryReqDto
				{
					FileName = images[0].FileName,
					FileStream = images[0].FileStream,
					imageType = Domain.Enums.ImageType.DriverProfilePhoto
				},
				new CloudinaryReqDto
				{
					FileName = images[1].FileName,
					FileStream = images[1].FileStream,
					imageType = Domain.Enums.ImageType.DriverLicenseFront
				},
				new CloudinaryReqDto
				{
					FileName = images[2].FileName,
					FileStream = images[2].FileStream,
					imageType = Domain.Enums.ImageType.DriverLicenseBack
				}
			};

			var uploadResult = await _cloudinaryService.UploadPhotosAsync(cloudinaryReqDtoList);
			if (uploadResult.IsFailure)
			{
				return Result<string?>.Failure(uploadResult.Errors);
			}

			try
			{	
				var driverProfile = new DriverProfile
				{
					UserId = userFound.Data!.Id,
					CreatedAt = DateTime.UtcNow,
					LicenseExpiryDate = driverDto.LicenseExpiryDate,
					Status=DriverStatus.Pending,
				};
				await _unitOfWork.DriverProfiles.AddAsync(driverProfile);
				await _unitOfWork.SaveChangesAsync(); 
				var imageEntities = uploadResult.Data!.Select(img => new Image
				{
					Url = img.ImageUrl!,
					PublicId = img.deleteId!,
					Type =img.ImageType,
					IsDeleted = false,
					CreatedAt = DateTime.UtcNow,
					DriverProfileId = driverProfile.Id
				}).ToList();

				await _unitOfWork.Images.AddRangeAsync(imageEntities);
				await _unitOfWork.SaveChangesAsync();

				return Result<string?>.Success(userFound.Data.Id);
			}
			catch
			{
				throw;
			}
		}
	}
}
