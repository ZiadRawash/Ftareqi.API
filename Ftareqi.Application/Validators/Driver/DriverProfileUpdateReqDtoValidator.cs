using FluentValidation;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.DriverRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Validators.Driver
{
	public class DriverProfileUpdateReqDtoValidator : AbstractValidator<DriverProfileUpdateReqDto>
	{
		public DriverProfileUpdateReqDtoValidator()
		{
			RuleFor(x => x)
				.Must(HaveAtLeastOneField)
				.WithMessage("At least one field must be provided for update");

			When(x => x.LicenseExpiryDate.HasValue, () =>
			{
				RuleFor(x => x.LicenseExpiryDate!.Value)
					.GreaterThan(DateTime.UtcNow.Date.AddDays(1))
					.WithMessage("License expiry date must be in the future");
			});

			When(x => x.DriverProfilePhoto != null, () =>
			{
				RuleFor(x => x.DriverProfilePhoto!.Length)
					.GreaterThan(0).WithMessage("Driver photo cannot be empty");
				RuleFor(x => x.DriverProfilePhoto!)
					.Must(ImageValidator.BeValidExtension)
					.WithMessage("Driver photo type is not allowed");
				RuleFor(x => x.DriverProfilePhoto!)
					.Must(ImageValidator.BeValidMimeType)
					.WithMessage("Driver photo type is invalid");
				RuleFor(x => x.DriverProfilePhoto!.Length)
					.LessThanOrEqualTo(ImageValidator.MaxFileSize)
					.WithMessage("Driver photo exceeds file size limit (5MB)");
			});

			When(x => x.DriverLicenseFront != null, () =>
			{
				RuleFor(x => x.DriverLicenseFront!.Length)
					.GreaterThan(0).WithMessage("Driver license front cannot be empty");
				RuleFor(x => x.DriverLicenseFront!)
					.Must(ImageValidator.BeValidExtension)
					.WithMessage("Driver license front type is not allowed");
				RuleFor(x => x.DriverLicenseFront!)
					.Must(ImageValidator.BeValidMimeType)
					.WithMessage("Driver license front type is invalid");
				RuleFor(x => x.DriverLicenseFront!.Length)
					.LessThanOrEqualTo(ImageValidator.MaxFileSize)
					.WithMessage("Driver license front exceeds file size limit (5MB)");
			});

			When(x => x.DriverLicenseBack != null, () =>
			{
				RuleFor(x => x.DriverLicenseBack!.Length)
					.GreaterThan(0).WithMessage("Driver license back cannot be empty");
				RuleFor(x => x.DriverLicenseBack!)
					.Must(ImageValidator.BeValidExtension)
					.WithMessage("Driver license back type is not allowed");
				RuleFor(x => x.DriverLicenseBack!)
					.Must(ImageValidator.BeValidMimeType)
					.WithMessage("Driver license back type is invalid");
				RuleFor(x => x.DriverLicenseBack!.Length)
					.LessThanOrEqualTo(ImageValidator.MaxFileSize)
					.WithMessage("Driver license back exceeds file size limit (5MB)");
			});

		}
		private bool HaveAtLeastOneField(DriverProfileUpdateReqDto dto)
		{
			return dto.LicenseExpiryDate.HasValue ||
				   dto.DriverProfilePhoto != null ||
				   dto.DriverLicenseFront != null ||
				   dto.DriverLicenseBack != null;
		}
	}
}
