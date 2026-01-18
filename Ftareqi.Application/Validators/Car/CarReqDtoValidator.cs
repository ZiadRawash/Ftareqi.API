using FluentValidation;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.DTOs.DriverRegistration;
using System;

namespace Ftareqi.Application.Validators.Car
{
	public class CarReqDtoValidator : AbstractValidator<CarReqDto>
	{
		public CarReqDtoValidator()
		{
			// Number of seats
			RuleFor(x => x.NumOfSeats)
				.NotEmpty().WithMessage("Number of seats is required.")
				.GreaterThan(0).WithMessage("Seats must be greater than 0.")
				.LessThanOrEqualTo(5).WithMessage("Maximum allowed seats is 5.");

			// Plate
			RuleFor(x => x.Plate)
				.NotEmpty().WithMessage("Plate number is required.")
				.MinimumLength(3).WithMessage("Plate number is too short.")
				.MaximumLength(15).WithMessage("Plate number is too long.");

			// Model
			RuleFor(x => x.Model)
				.NotEmpty().WithMessage("Model is required.")
				.MinimumLength(2).WithMessage("Model is too short.")
				.MaximumLength(30).WithMessage("Model is too long.");

			// Color
			RuleFor(x => x.Color)
				.NotEmpty().WithMessage("Color is required.")
				.MaximumLength(30).WithMessage("Color is too long.");

			// Car Photo
			RuleFor(x => x.CarPhoto)
				.NotNull().WithMessage("Car photo is required.")
				.DependentRules(() =>
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

			// Car License Front
			RuleFor(x => x.CarLicenseFront)
				.NotNull().WithMessage("Front car license photo is required.")
				.DependentRules(() =>
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

			// Car License Back
			RuleFor(x => x.CarLicenseBack)
				.NotNull().WithMessage("Back car license photo is required.")
				.DependentRules(() =>
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

			// License expiry date
			RuleFor(x => x.LicenseExpiryDate)
				.GreaterThan(DateTime.UtcNow.Date.AddDays(1))
				.WithMessage("License expiry date must be in the future");
		}
	}
}
