using FluentValidation;
using Ftareqi.Application.DTOs.Rides;

namespace Ftareqi.Application.Validators.Rides
{
	public class RideSearchRequestDtoValidator : AbstractValidator<RideSearchRequestDto>
	{
		public RideSearchRequestDtoValidator()
		{
			RuleFor(x => x.Criteria)
				.SetValidator(new RideCriteriaValidator()!)
				.When(x => x.Criteria != null);
		}
	}

	public class RideCriteriaValidator : AbstractValidator<RideCriteria>
	{
		public RideCriteriaValidator()
		{
			RuleFor(x => x.StartLatitude)
				.NotNull().WithMessage("Start latitude is required.")
				.Must(x => x >= -90 && x <= 90).WithMessage("Start latitude must be between -90 and 90.");

			RuleFor(x => x.StartLongitude)
				.NotNull().WithMessage("Start longitude is required.")
				.Must(x => x >= -180 && x <= 180).WithMessage("Start longitude must be between -180 and 180.");

			RuleFor(x => x.EndLatitude)
				.NotNull().WithMessage("End latitude is required.")
				.Must(x => x >= -90 && x <= 90).WithMessage("End latitude must be between -90 and 90.");

			RuleFor(x => x.EndLongitude)
				.NotNull().WithMessage("End longitude is required.")
				.Must(x => x >= -180 && x <= 180).WithMessage("End longitude must be between -180 and 180.");

			RuleFor(x => x.Seats)
				.NotNull().WithMessage("Seats are required.")
				.Must(x => x >= 1 && x <= 5).WithMessage("Seats must be between 1 and 5.");

			RuleFor(x => x.DepartureTime)
				.NotNull().WithMessage("Departure time is required.")
				.Must(x => x != default)
				.WithMessage("Departure time is required.");
		}
	}
}
