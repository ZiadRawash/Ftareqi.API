using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Mappers
{
	public static class DriverProfileMapper
	{
		public static DriverProfileCreateDto ToCreateDto(this DriverProfileReqDto dto, string userId)
		{
			return new DriverProfileCreateDto
			{
				UserId = userId,
				DriverProfilePhoto = dto.DriverProfilePhoto,
				DriverLicenseFront = dto.DriverLicenseFront,
				DriverLicenseBack = dto.DriverLicenseBack,
				LicenseExpiryDate = dto.LicenseExpiryDate
			};
		}

		public static CarCreateDto ToCreateDto(this CarReqDto dto, string userId)
		{
			return new CarCreateDto
			{
				UserId = userId,
				Model = dto.Model,
				Color = dto.Color,
				Palette = dto.Plate,
				NumOfSeats = dto.NumOfSeats,
				CarPhoto = dto.CarPhoto,
				CarLicenseFront = dto.CarLicenseFront,
				CarLicenseBack = dto.CarLicenseBack,
				LicenseExpiryDate= dto.LicenseExpiryDate
				
			};
		}
		public static DriverWithCarResponseDto ToDto(DriverProfile profile)
		{
			var car = profile.Car;

			return new DriverWithCarResponseDto
			{
				ProfileId= profile.Id,
				FullName = profile.User?.FullName,
				PhoneNumber = profile.User?.PhoneNumber,
				DriverStatus = profile.Status,
				DriverLicenseExpiryDate = profile.LicenseExpiryDate,
				ProfileCreationDate = profile.CreatedAt,
				DriverPhoto = GetImageUrl(profile.Images, ImageType.DriverProfilePhoto),
				DriverLicenseFront = GetImageUrl(profile.Images, ImageType.DriverLicenseFront),
				DriverLicenseBack = GetImageUrl(profile.Images, ImageType.DriverLicenseBack),

				// car info
				CarLicenseExpiryDate=car?.LicenseExpiryDate,
				Model = car?.Model ?? "",
				Color = car?.Color,
				Plate = car?.Plate,
				NumOfSeats = car?.NumOfSeats ?? 0,
				CarPhoto = GetImageUrl(car?.Images, ImageType.CarPhoto),
				CarLicenseFront = GetImageUrl(car?.Images, ImageType.CarLicenseFront),
				CarLicenseBack = GetImageUrl(car?.Images, ImageType.CarLicenseBack)
			};
		}

		private static string? GetImageUrl(IEnumerable<Image>? images, ImageType type)
		{
			return images?.FirstOrDefault(i => i.Type == type)?.Url;
		}
	}

}
