using FluentValidation;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.DriverRegistration;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ftareqi.Application.Validators.Driver
{
	public class DriverProfileReqDtoValidator : AbstractValidator<DriverProfileReqDto>
	{


		public DriverProfileReqDtoValidator()
		{
			// Driver Profile Photo
			RuleFor(x => x.DriverProfilePhoto)
				.NotNull().WithMessage("Driver photo is required.")
				.DependentRules(() =>
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

			// Driver License Front
			RuleFor(x => x.DriverLicenseFront)
				.NotNull().WithMessage("Driver license front photo is required.")
				.DependentRules(() =>
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

			// Driver License Back
			RuleFor(x => x.DriverLicenseBack)
				.NotNull().WithMessage("Driver license back photo is required.")
				.DependentRules(() =>
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

			// License expiry date
			RuleFor(x => x.LicenseExpiryDate)
				.GreaterThan(DateTime.UtcNow.Date.AddDays(1))
				.WithMessage("License expiry date must be in the future");
		}

	}
}
