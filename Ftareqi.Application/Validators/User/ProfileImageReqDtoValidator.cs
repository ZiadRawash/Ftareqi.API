using FluentValidation;
using Ftareqi.Application.DTOs.Profile;
using Ftareqi.Application.Common.Helpers;

namespace Ftareqi.Application.Validators.User
{
	public class ProfileImageReqDtoValidator : AbstractValidator<ProfileImageReqDto>
	{
		public ProfileImageReqDtoValidator()
		{
			RuleFor(x => x.Image)
				.Cascade(CascadeMode.Stop)
				.NotNull().WithMessage("Image is required")
				.NotEmpty().WithMessage("Image is required")
				.Must(ImageValidator.BeValidExtension)
					.WithMessage("Invalid image extension")
				.Must(ImageValidator.BeValidMimeType)
					.WithMessage("Invalid image type")
				.Must(x => x!.Length <= ImageValidator.MaxFileSize)
					.WithMessage("Image exceeds file size limit (5MB)");
		}
	}
}
