using FluentValidation;
using Ftareqi.Application.DTOs.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Validators.Auth
{
	public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
	{
		public ChangePasswordDtoValidator()
		{
			RuleFor(x => x.UserId)
				.NotEmpty().WithMessage("UserId is required.");

			RuleFor(x => x.OldPassword)
				.NotEmpty().WithMessage("Old password is required.");

			RuleFor(x => x.NewPassword)
				.NotEmpty().WithMessage("New password is required.")
				.MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
				.Must((dto, newPassword) => newPassword != dto.OldPassword)
					.WithMessage("New password cannot be the same as the old password.");
		}
	}
}
