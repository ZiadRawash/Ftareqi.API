using FluentValidation;
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
		private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
		private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/webp" };
		private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

		public DriverProfileReqDtoValidator()
		{
			RuleFor(x => x.PhoneNumber)
				.NotEmpty().WithMessage("Phone number is required.")
				.Must(phone => string.IsNullOrEmpty(phone) || Regex.IsMatch(
					phone.Replace("\u202A", "")
						 .Replace("\u202C", "")
						 .Replace("\u200E", "")
						 .Replace("\u200F", "")
						 .Trim(),
					@"^\+\d{10,15}$"));

			RuleFor(x => x.DriverProfilePhoto)
				.NotNull().WithMessage("Driver photo is required.")
				.Must(f => f!.Length > 0).WithMessage("Driver photo cannot be empty.")
				.Must(BeValidExtension).WithMessage("Driver photo type is not allowed.")
				.Must(BeValidMimeType).WithMessage("Driver photo type is invalid.")
				.Must(f => f!.Length <= MaxFileSize).WithMessage("Driver photo exceeds file size limit (5MB).");

			RuleFor(x => x.DriverLicenseFront)
				.NotNull().WithMessage("Driver photo is required.")
				.Must(f => f!.Length > 0).WithMessage("Driver photo cannot be empty.")
				.Must(BeValidExtension).WithMessage("Driver photo type is not allowed.")
				.Must(BeValidMimeType).WithMessage("Driver photo type is invalid.")
				.Must(f => f!.Length <= MaxFileSize).WithMessage("Driver photo exceeds file size limit (5MB).");

			RuleFor(x => x.DriverLicenseBack)
				.NotNull().WithMessage("Driver photo is required.")
				.Must(f => f!.Length > 0).WithMessage("Driver photo cannot be empty.")
				.Must(BeValidExtension).WithMessage("Driver photo type is not allowed.")
				.Must(BeValidMimeType).WithMessage("Driver photo type is invalid.")
				.Must(f => f!.Length <= MaxFileSize).WithMessage("Driver photo exceeds file size limit (5MB).");


			RuleFor(x => x.LicenseExpiryDate)
				.GreaterThan(DateTime.UtcNow.Date)
				.WithMessage("License expiry date must be in the future.");
		}
		private bool BeValidExtension(IFormFile? file)
		{
			if (file == null) return false;
			var ext = Path.GetExtension(file.FileName).ToLower();
			return _allowedExtensions.Contains(ext);
		}

		private bool BeValidMimeType(IFormFile? file)
		{
			if (file == null) return false;
			return _allowedMimeTypes.Contains(file.ContentType);
		}
	}
}
