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
			RuleFor(x => x.DriverProfilePhoto)
				.NotNull().WithMessage("Driver photo is required.")
				.Must(f => f!.Length > 0).WithMessage("Driver photo cannot be empty.")
				.Must(ImageValidator.BeValidExtension).WithMessage("Driver photo type is not allowed.")
				.Must(ImageValidator.BeValidMimeType).WithMessage("Driver photo type is invalid.")
				.Must(f => f!.Length <= ImageValidator.MaxFileSize).WithMessage("Driver photo exceeds file size limit (5MB).");

			RuleFor(x => x.DriverLicenseFront)
				.NotNull().WithMessage("Driver photo is required.")
				.Must(f => f!.Length > 0).WithMessage("Driver photo cannot be empty.")
				.Must(ImageValidator.BeValidExtension).WithMessage("Driver photo type is not allowed.")
				.Must(ImageValidator.BeValidMimeType).WithMessage("Driver photo type is invalid.")
				.Must(f => f!.Length <= ImageValidator.MaxFileSize).WithMessage("Driver photo exceeds file size limit (5MB).");

			RuleFor(x => x.DriverLicenseBack)
				.NotNull().WithMessage("Driver photo is required.")
				.Must(f => f!.Length > 0).WithMessage("Driver photo cannot be empty.")
				.Must(ImageValidator.BeValidExtension).WithMessage("Driver photo type is not allowed.")
				.Must(ImageValidator.BeValidMimeType).WithMessage("Driver photo type is invalid.")
				.Must(f => f!.Length <= ImageValidator.MaxFileSize).WithMessage("Driver photo exceeds file size limit (5MB).");


			RuleFor(x => x.LicenseExpiryDate)
				.GreaterThan(DateTime.UtcNow.Date.AddDays(1))
				.WithMessage("License expiry date must be in the future.");
		}
	}
}
