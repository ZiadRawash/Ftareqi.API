using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Review
{
	public class CreateReviewDto
	{
		[Required(ErrorMessage = "RideBookingId is required.")]
		public int RideBookingId { get; set; }

		[StringLength(500, ErrorMessage = "Review text cannot exceed 500 characters.")]
		public string? TextReview { get; set; }

		[Required]
		[Range(.5, 5, ErrorMessage = "Stars must be between .5 and 5.")]
		public float Stars { get; set; }
	}
}
