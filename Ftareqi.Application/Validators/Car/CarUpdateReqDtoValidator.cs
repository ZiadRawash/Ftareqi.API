using FluentValidation;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.DriverRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Validators.Car
{
	public class CarUpdateReqDtoValidator : AbstractValidator<CarUpdateReqDto>
	{
		public CarUpdateReqDtoValidator()
		{
			RuleFor(x => x)
				.Must(HaveAtLeastOneField)
				.WithMessage("At least one field must be provided for update");

			When(x => !string.IsNullOrWhiteSpace(x.Color), () =>
			{
				RuleFor(x => x.Color!)
					.MaximumLength(30)
					.WithMessage("Color is too long");
			});

			When(x => !string.IsNullOrWhiteSpace(x.Model), () =>
			{
				RuleFor(x => x.Model!)
					.MinimumLength(2).WithMessage("Model is too short")
					.MaximumLength(30).WithMessage("Model is too long");
			});

			When(x => !string.IsNullOrWhiteSpace(x.Plate), () =>
			{
				RuleFor(x => x.Plate!)
					.MinimumLength(3).WithMessage("Plate number is too short")
					.MaximumLength(15).WithMessage("Plate number is too long");
			});

			When(x => x.NumOfSeats.HasValue, () =>
			{
				RuleFor(x => x.NumOfSeats!.Value)
					.GreaterThan(0).WithMessage("Seats must be greater than 0")
					.LessThanOrEqualTo(5).WithMessage("Maximum allowed seats is 5");
			});

			When(x => x.LicenseExpiryDate.HasValue, () =>
			{
				RuleFor(x => x.LicenseExpiryDate!.Value)
					.GreaterThan(DateTime.UtcNow.Date.AddDays(1))
					.WithMessage("License expiry date must be in the future");
			});

			When(x => x.CarPhoto != null, () =>
			{
				RuleFor(x => x.CarPhoto!.Length)
					.GreaterThan(0).WithMessage("Car photo cannot be empty");
				RuleFor(x => x.CarPhoto!)
					.Must(ImageValidator.BeValidExtension)
					.WithMessage("Car photo type is not allowed");
				RuleFor(x => x.CarPhoto!)
					.Must(ImageValidator.BeValidMimeType)
					.WithMessage("Car photo type is invalid");
				RuleFor(x => x.CarPhoto!.Length)
					.LessThanOrEqualTo(ImageValidator.MaxFileSize)
					.WithMessage("Car photo exceeds file size limit (5MB)");
			});

			When(x => x.CarLicenseFront != null, () =>
			{
				RuleFor(x => x.CarLicenseFront!.Length)
					.GreaterThan(0).WithMessage("Front car license photo cannot be empty");
				RuleFor(x => x.CarLicenseFront!)
					.Must(ImageValidator.BeValidExtension)
					.WithMessage("Front license type is not allowed");
				RuleFor(x => x.CarLicenseFront!)
					.Must(ImageValidator.BeValidMimeType)
					.WithMessage("Front license type is invalid");
				RuleFor(x => x.CarLicenseFront!.Length)
					.LessThanOrEqualTo(ImageValidator.MaxFileSize)
					.WithMessage("Front license photo exceeds size limit (5MB)");
			});

			When(x => x.CarLicenseBack != null, () =>
			{
				RuleFor(x => x.CarLicenseBack!.Length)
					.GreaterThan(0).WithMessage("Back car license photo cannot be empty");
				RuleFor(x => x.CarLicenseBack!)
					.Must(ImageValidator.BeValidExtension)
					.WithMessage("Back license type is not allowed");
				RuleFor(x => x.CarLicenseBack!)
					.Must(ImageValidator.BeValidMimeType)
					.WithMessage("Back license type is invalid");
				RuleFor(x => x.CarLicenseBack!.Length)
					.LessThanOrEqualTo(ImageValidator.MaxFileSize)
					.WithMessage("Back license photo exceeds size limit (5MB)");
			});
		}

		private bool HaveAtLeastOneField(CarUpdateReqDto dto)
		{
			return !string.IsNullOrWhiteSpace(dto.Color) ||
				   !string.IsNullOrWhiteSpace(dto.Model) ||
				   !string.IsNullOrWhiteSpace(dto.Plate) ||
				   dto.NumOfSeats.HasValue ||
				   dto.LicenseExpiryDate.HasValue ||
				   dto.CarPhoto != null ||
				   dto.CarLicenseBack != null ||
				   dto.CarLicenseFront != null;
		}
	}
}
