using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.DTOs.Files;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Models;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class DriverService : IDriverService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserService _userService;
		private readonly ICloudinaryService _cloudinaryService;
		private readonly IFileMapper _fileMapper;
		public DriverService(IUnitOfWork unitOfWork,
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
			if (userFound.IsFailure)
				return Result<string?>.Failure(userFound.Errors);
			var driverPhotoStream = _fileMapper.MapFiles([driverDto.DriverPhoto]);
			var licensePhotoStream = _fileMapper.MapFiles([driverDto.DriverLicensePhoto]);
			var driverPhotoUrl = await _cloudinaryService.UploadPhoto(new CloudinaryReqDto { FileName = driverPhotoStream[0].FileName, FileStream = driverPhotoStream[0].FileStream });
			if (driverPhotoUrl.IsFailure)
				return Result<string?>.Failure(driverPhotoUrl.Errors);

			var licensePhotoUrl = await _cloudinaryService.UploadPhoto(new CloudinaryReqDto { FileName = licensePhotoStream[0].FileName, FileStream = licensePhotoStream[0].FileStream });
			if (licensePhotoUrl.IsFailure)
				return Result<string?>.Failure(licensePhotoUrl.Errors);
			var driverProfile = new DriverProfile
			{
				UserId = userFound.Data!.Id,
				CreatedAt = DateTime.UtcNow,
				DriverPhotoUrl = driverPhotoUrl.Data!.ImageUrl!,
				DriverLicensePhotoUrl = licensePhotoUrl.Data!.ImageUrl!,
				LicenseExpiryDate=driverDto.LicenseExpiryDate,
			};
			await _unitOfWork.DriverProfiles.AddAsync(driverProfile);
			await _unitOfWork.SaveChangesAsync();
			return Result<string?>.Success(userFound.Data!.Id);
		}
	}
}
