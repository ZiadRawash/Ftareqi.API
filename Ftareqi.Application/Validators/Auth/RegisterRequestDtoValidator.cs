using FluentValidation;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Validators.Auth
{
	public class RegisterRequestDtoValidator: AbstractValidator<RegisterRequestDto>
	{
		private readonly IUnitOfWork _unitOfWork;
		public RegisterRequestDtoValidator(IUnitOfWork unitOfWork)
		{
			_unitOfWork=unitOfWork;

			RuleFor(x => x.FullName)
				.NotEmpty().WithMessage("Full name is required.")
				.Length(2, 100).WithMessage("Full name must be between 2 and 100 characters.")
				.Matches(@"^[a-zA-Z\s'-]+$").WithMessage("Full name can only contain letters, spaces, hyphens, and apostrophes.");

			RuleFor(x => x.Password)
				.NotEmpty().WithMessage("Password is required.")
				.MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
				.Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])")
				.WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");

			RuleFor(x => x.PhoneNumber)
			   .NotEmpty().WithMessage("Phone number is required.")
			   .Matches(@"^\+?\d{10,15}$").WithMessage("Invalid phone number format.")
			   .MustAsync(IsPhoneNumberUnique).WithMessage("This phone number is already registered.");

			RuleFor(x => x.Gender)
				.NotNull().WithMessage("Gender is required.")
				.IsInEnum().WithMessage("Invalid gender value.");

			RuleFor(x => x.DateOfBirth)
			   .NotEmpty().WithMessage("Date of birth is required.")
			   .Must(BeAValidDate).WithMessage("Invalid date format.")
			   .Must(BeAtLeast18YearsOld).WithMessage("You must be at least 18 years old to register.")
			   .Must(BeRealisticAge).WithMessage("Date of birth must be between 18 and 80 years ago.");
		
		}

		private bool BeAValidDate(DateTime dateOfBirth)
			=>	dateOfBirth != default && dateOfBirth < DateTime.UtcNow;

		private bool BeAtLeast18YearsOld(DateTime dateOfBirth)
		{
			var today = DateTime.UtcNow;
			var age = today.Year - dateOfBirth.Year;
			if (dateOfBirth.Date > today.AddYears(-age))
				age--;
			if (age >= 18) return true;
			else return false;
		}
		private bool BeRealisticAge(DateTime dateOfBirth)
		{
			var today = DateTime.UtcNow;
			var age = today.Year - dateOfBirth.Year;
			if (dateOfBirth.Date > today.AddYears(-age))
				age--;
			if (age >= 18 && age<80) return true;
			else return false;
		}

		private async Task<bool> IsPhoneNumberUnique(string phoneNumber, CancellationToken cancellationToken)
						=>!await _unitOfWork.Users.ExistsAsync(x => x.PhoneNumber == phoneNumber);
		
	}
}
