using Ftareqi.Application.DTOs.DriverRegistration;
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
				Palette = dto.palette,
				NumOfSeats = dto.NumOfSeats,
				CarPhoto = dto.CarPhoto,
				CarLicenseFront = dto.CarLicenseFront,
				CarLicenseBack = dto.CarLicenseBack
			};
		}
	}

}
