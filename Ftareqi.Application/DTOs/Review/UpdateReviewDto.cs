using System.ComponentModel.DataAnnotations;

namespace Ftareqi.Application.DTOs.Review
{
	public class UpdateReviewDto
	{
		[StringLength(500, ErrorMessage = "Review text cannot exceed 500 characters.")]
		public string? TextReview { get; set; }

		[Required]
		[Range(.5, 5, ErrorMessage = "Stars must be between .5 and 5.")]
		public float Stars { get; set; }
	}
}
