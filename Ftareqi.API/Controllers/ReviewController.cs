using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs.Review;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ftareqi.API.Controllers
{
	[Authorize]
	[ApiController]
	[Route("api/reviews")]
	public class ReviewController : ControllerBase
	{
		private readonly IReviewService _reviewService;

		public ReviewController(IReviewService reviewService)
		{
			_reviewService = reviewService;
		}

		/// <summary>
		/// Creates a new review.
		/// </summary>
		[HttpPost]
		public async Task<ActionResult<ApiResponse>> CreateReview([FromBody] CreateReviewDto request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var result = await _reviewService.CreateReview(request);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors
			});
		}

		/// <summary>
		/// Updates an existing review.
		/// </summary>
		[HttpPut("{reviewId:int}")]
		public async Task<ActionResult<ApiResponse>> UpdateReview(int reviewId, [FromBody] UpdateReviewDto request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState.ToApiResponse());

			var result = await _reviewService.UpdateReview(reviewId, request);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors
			});
		}

		/// <summary>
		/// Deletes an existing review.
		/// </summary>
		[HttpDelete("{reviewId:int}")]
		public async Task<ActionResult<ApiResponse>> DeleteReview(int reviewId)
		{
			var result = await _reviewService.DeleteReview(reviewId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors
			});
		}

		/// <summary>
		/// Retrieves all reviews for a specific driver profile.
		/// </summary>
		[HttpGet("driver/{driverProfileId:int}")]
		public async Task<ActionResult<ApiResponse<List<GetReviewsDto>>>> GetDriverReviews(int driverProfileId)
		{
			var result = await _reviewService.GetDriverReviews(driverProfileId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<List<GetReviewsDto>>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Retrieves all reviews for a specific ride.
		/// </summary>
		[HttpGet("ride/{rideId:int}/all")]
		public async Task<ActionResult<ApiResponse<List<GetReviewsDto>>>> GetRideReviews(int rideId)
		{
			var result = await _reviewService.GetRideReviews(rideId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<List<GetReviewsDto>>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}

		/// <summary>
		/// Retrieves a review by ride booking id.
		/// </summary>
		[HttpGet("ride-booking/{rideBookingId:int}")]
		public async Task<ActionResult<ApiResponse<GetReviewsDto>>> GetReviewByRideBookingId(int rideBookingId)
		{
			var result = await _reviewService.GetReviewByRideBookingId(rideBookingId);
			if (result.IsFailure)
			{
				return BadRequest(new ApiResponse
				{
					Success = false,
					Message = result.Message,
					Errors = result.Errors
				});
			}

			return Ok(new ApiResponse<GetReviewsDto>
			{
				Success = true,
				Message = result.Message,
				Errors = result.Errors,
				Data = result.Data
			});
		}
	}
}
