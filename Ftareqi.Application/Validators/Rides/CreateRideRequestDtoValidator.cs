using FluentValidation;
using Ftareqi.Application.DTOs.Rides;

namespace Ftareqi.Application.Validators.Rides
{
	public class CreateRideRequestDtoValidator : AbstractValidator<CreateRideRequestDto>
	{
		public CreateRideRequestDtoValidator()
		{
			RuleFor(x => x.StartLatitude)
				.InclusiveBetween(-90, 90).WithMessage("Start latitude must be between -90 and 90.");

			RuleFor(x => x.StartLongitude)
				.InclusiveBetween(-180, 180).WithMessage("Start longitude must be between -180 and 180.");

			RuleFor(x => x.StartAddress)
				.NotNull().WithMessage("Start address must not be null.")
				.NotEmpty().WithMessage("Start address is required.")
				.MaximumLength(300).WithMessage("Start address length must not exceed 300 characters.");

			RuleFor(x => x.EndLatitude)
				.InclusiveBetween(-90, 90).WithMessage("End latitude must be between -90 and 90.");

			RuleFor(x => x.EndLongitude)
				.InclusiveBetween(-180, 180).WithMessage("End longitude must be between -180 and 180.");

			RuleFor(x => x.EndAddress)
				.NotNull().WithMessage("End address must not be null.")
				.NotEmpty().WithMessage("End address is required.")
				.MaximumLength(300).WithMessage("End address length must not exceed 300 characters.");

			RuleFor(x => x.DepartureTime)
				.Must(departureTime => departureTime != default && departureTime > DateTime.UtcNow)
				.WithMessage("Departure time must be in the future.");

			RuleFor(x => x.TotalSeats)
				.InclusiveBetween(1, 5).WithMessage("Total seats must be between 1 and 5.");

			RuleFor(x => x.PricePerSeat)
				.GreaterThan(0).WithMessage("Price per seat must be greater than 0.")
				.LessThanOrEqualTo(10000).WithMessage("Price per seat must be less than or equal to 10000.");

			RuleFor(x => x.WaitingTimeMinutes)
				.InclusiveBetween(0, 120).WithMessage("Waiting time must be between 0 and 120 minutes.");

			RuleFor(x => x.RidePreferences)
				.NotNull().WithMessage("Ride preferences are required.")
				.SetValidator(new CreateRidePreferencesRequestDtoValidator()!);
		}
	}

	public class CreateRidePreferencesRequestDtoValidator : AbstractValidator<CreateRidePreferencesRequestDto>
	{
		public CreateRidePreferencesRequestDtoValidator()
		{
		}
	}
}
