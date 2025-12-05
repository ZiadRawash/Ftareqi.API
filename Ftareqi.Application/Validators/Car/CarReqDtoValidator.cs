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

			RuleFor(x => x.NumOfSeats)
				.NotEmpty().WithMessage("Number of seats is required.")
				.GreaterThan(0).WithMessage("Seats must be greater than 0.")
				.LessThanOrEqualTo(5).WithMessage("Maximum allowed seats is 5.");

			RuleFor(x => x.palette)
				.NotEmpty().WithMessage("Plate number is required.")
				.MinimumLength(3).WithMessage("Plate number is too short.")
				.MaximumLength(15).WithMessage("Plate number is too long.");

			RuleFor(x => x.Model)
				.NotEmpty().WithMessage("Model is required.")
				.MinimumLength(2)
				.MaximumLength(30);

			RuleFor(x => x.Color)
				.NotEmpty().WithMessage("Color is required.")
				.MaximumLength(30);

			RuleFor(x => x.CarPhoto)
				.NotNull().WithMessage("Car photo is required.")
				.Must(f => f!.Length > 0).WithMessage("Car photo cannot be empty.")
				.Must(ImageValidator.BeValidExtension).WithMessage("Car photo type is not allowed.")
				.Must(ImageValidator.BeValidMimeType).WithMessage("Car photo type is invalid.")
				.Must(f => f!.Length <= ImageValidator.MaxFileSize).WithMessage("Car photo exceeds file size limit (5MB).");

			RuleFor(x => x.CarLicenseFront)
				.NotNull().WithMessage("Front car license photo is required.")
				.Must(f => f!.Length > 0).WithMessage("Front car license photo cannot be empty.")
				.Must(ImageValidator.BeValidExtension).WithMessage("Front license type is not allowed.")
				.Must(ImageValidator.BeValidMimeType).WithMessage("Front license type is invalid.")
				.Must(f => f!.Length <= ImageValidator.MaxFileSize).WithMessage("Front license photo exceeds size limit (5MB).");

			RuleFor(x => x.CarLicenseBack)
				.NotNull().WithMessage("Back car license photo is required.")
				.Must(f => f!.Length > 0).WithMessage("Back car license photo cannot be empty.")
				.Must(ImageValidator.BeValidExtension).WithMessage("Back license type is not allowed.")
				.Must(ImageValidator.BeValidMimeType).WithMessage("Back license type is invalid.")
				.Must(f => f!.Length <= ImageValidator.MaxFileSize).WithMessage("Back license photo exceeds size limit (5MB).");

			RuleFor(x => x.LicenseExpiryDate)
				.GreaterThan(DateTime.UtcNow.Date.AddDays(1))
				.WithMessage("License expiry date must be in the future.");
		}
	}
}
