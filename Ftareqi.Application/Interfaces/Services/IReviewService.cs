using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Review;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	/// <summary>
	/// Defines operations for managing ride reviews.
	/// </summary>
	public interface IReviewService
	{
		/// <summary>
		/// Retrieves all reviews for a specific driver.
		/// </summary>
		/// <param name="driverProfileId">The identifier of the driver profile.</param>
		/// <returns>A result containing all reviews for the driver.</returns>
		public Task<Result<List<GetReviewsDto>>> GetDriverReviews(int driverProfileId);

		/// <summary>
		/// Creates a new review.
		/// </summary>
		/// <param name="model">The review data to create.</param>
		/// <returns>A result indicating success or failure of the operation.</returns>
		public Task<Result> CreateReview (CreateReviewDto model);

		/// <summary>
		/// Updates an existing review.
		/// </summary>
		/// <param name="reviewId">The identifier of the review to update.</param>
		/// <param name="model">The review fields to update.</param>
		/// <returns>A result indicating success or failure of the operation.</returns>
		public Task<Result> UpdateReview (int reviewId, UpdateReviewDto model);

		/// <summary>
		/// Deletes an existing review.
		/// </summary>
		/// <param name="reviewId">The identifier of the review to delete.</param>
		/// <returns>A result indicating success or failure of the operation.</returns>
		public Task<Result> DeleteReview (int reviewId);

		/// <summary>
		/// Retrieves all reviews associated with a specific ride.
		/// </summary>
		/// <param name="rideId">The identifier of the ride.</param>
		/// <returns>A result containing all ride review details.</returns>
		public Task<Result<List<GetReviewsDto>>> GetRideReviews (int rideId);

		/// <summary>
		/// Retrieves the review associated with a specific ride booking.
		/// </summary>
		/// <param name="rideBookingId">The identifier of the ride booking.</param>
		/// <returns>A result containing the ride booking review details.</returns>
		public Task<Result<GetReviewsDto>> GetReviewByRideBookingId(int rideBookingId);
	}
}
