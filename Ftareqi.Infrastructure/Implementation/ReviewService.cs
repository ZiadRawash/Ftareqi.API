using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Review;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class ReviewService : IReviewService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<ReviewService> _logger;
		public ReviewService(IUnitOfWork unitOfWork, ILogger<ReviewService> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}
		public async Task<Result> CreateReview(CreateReviewDto model)
		{
			if (model == null)
			{
				_logger.LogWarning("CreateReview called with null model");
				return Result.Failure("Review data is required");
			}

			if (model.RideBookingId <= 0)
			{
				_logger.LogWarning("CreateReview called with invalid RideBookingId {RideBookingId}", model.RideBookingId);
				return Result.Failure("Valid RideBookingId is required");
			}

			try
			{
				var booking = await _unitOfWork.RideBookings.FirstOrDefaultAsync(
					x => x.Id == model.RideBookingId,
					x => x.Ride,
					x => x.Review!);

				if (booking == null || booking.Ride == null)
				{
					_logger.LogWarning("CreateReview: ride booking {RideBookingId} not found", model.RideBookingId);
					return Result.Failure("Ride booking not found");
				}

				if (booking.Review != null)
				{
					_logger.LogWarning("CreateReview: review already exists for ride booking {RideBookingId}", model.RideBookingId);
					return Result.Failure("A review already exists for this ride booking");
				}

				var now = DateTime.UtcNow;
				var review = new Review
				{
					RideBookingId = booking.Id,
					DriverProfileId = booking.Ride.DriverProfileId,
					TextReview = string.IsNullOrWhiteSpace(model.TextReview) ? null : model.TextReview.Trim(),
					Stars = model.Stars,
					CreatedAt = now,
					UpdatedAt = now
				};

				await _unitOfWork.Reviews.AddAsync(review);
				var driverProfile = await _unitOfWork.DriverProfiles.FirstOrDefaultAsync(x => x.Id == review.DriverProfileId);
				if (driverProfile == null)
				{
					_logger.LogWarning("CreateReview: driver profile {DriverProfileId} not found", review.DriverProfileId);
					return Result.Failure("Driver profile not found");
				}

				driverProfile.RatingCount += 1;
				driverProfile.RatingSum += review.Stars;
				driverProfile.UpdatedAt = now;
				_unitOfWork.DriverProfiles.Update(driverProfile);

				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Review created successfully for ride booking {RideBookingId}", model.RideBookingId);
				return Result.Success("Review created successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while creating review for ride booking {RideBookingId}", model.RideBookingId);
				return Result.Failure("Unexpected error happened while creating review");
			}
		}
		public async Task<Result> DeleteReview(int reviewId)
		{
			if (reviewId <= 0)
			{
				_logger.LogWarning("DeleteReview called with invalid review id {ReviewId}", reviewId);
				return Result.Failure("Valid ReviewId is required");
			}

			var reviewFound = await _unitOfWork.Reviews.FirstOrDefaultAsync(x=>x.Id==reviewId);
			if (reviewFound == null)
				return Result.Failure("Invalid ReviewId");
			var driverProfile = await _unitOfWork.DriverProfiles.FirstOrDefaultAsync(x => x.Id == reviewFound.DriverProfileId);
			if (driverProfile == null)
			{
				_logger.LogWarning("DeleteReview: driver profile {DriverProfileId} not found", reviewFound.DriverProfileId);
				return Result.Failure("Driver profile not found");
			}
			 _unitOfWork.Reviews.Delete(reviewFound);
			driverProfile.RatingCount = Math.Max(0, driverProfile.RatingCount - 1);
			driverProfile.RatingSum = Math.Max(0f, driverProfile.RatingSum - reviewFound.Stars);
			driverProfile.UpdatedAt = DateTime.UtcNow;
			_unitOfWork.DriverProfiles.Update(driverProfile);

			await _unitOfWork.SaveChangesAsync();
			return Result.Success();
		}
		public async Task<Result<List<GetReviewsDto>>> GetDriverReviews(int driverId)
		{
			if (driverId <= 0)
			{
				_logger.LogWarning("GetDriverReviews called with invalid driver id {DriverId}", driverId);
				return Result<List<GetReviewsDto>>.Failure("Valid driver id is required");
			}

			try
			{
				var reviews = await _unitOfWork.Reviews.FindAllAsNoTrackingAsync(
					x => x.DriverProfileId == driverId,
					x => x.RideBooking);

				var items = reviews
					.OrderByDescending(x => x.CreatedAt)
					.Select(MapToGetReviewsDto)
					.ToList();

				return Result<List<GetReviewsDto>>.Success(items);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while getting reviews for driver {DriverId}", driverId);
				return Result<List<GetReviewsDto>>.Failure("Unexpected error happened while getting driver reviews");
			}
		}
		public async Task<Result<List<GetReviewsDto>>> GetRideReviews(int rideId)
		{
			if (rideId <= 0)
			{
				_logger.LogWarning("GetRideReviews called with invalid ride id {RideId}", rideId);
				return Result<List<GetReviewsDto>>.Failure("Valid ride id is required");
			}
			try
			{
				var reviews = await _unitOfWork.Reviews.FindAllAsNoTrackingAsync(
					x => x.RideBooking.RideId == rideId,
					x => x.RideBooking);

				var items = reviews
					.OrderByDescending(x => x.CreatedAt)
					.Select(MapToGetReviewsDto)
					.ToList();

				if (items.Count == 0)
				{
					_logger.LogWarning("GetRideReviews: no reviews found for ride {RideId}", rideId);
					return Result<List<GetReviewsDto>>.Failure("No reviews found for this ride");
				}

				return Result<List<GetReviewsDto>>.Success(items);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while getting ride review for ride {RideId}", rideId);
				return Result<List<GetReviewsDto>>.Failure("Unexpected error happened while getting ride reviews");
			}
		}
		public async Task<Result<GetReviewsDto>> GetReviewByRideBookingId(int rideBookingId)
		{
			if (rideBookingId <= 0)
			{
				_logger.LogWarning("GetReviewByRideBookingId called with invalid ride booking id {RideBookingId}", rideBookingId);
				return Result<GetReviewsDto>.Failure("Valid ride booking id is required");
			}

			try
			{
				var review = await _unitOfWork.Reviews.FirstOrDefaultAsNoTrackingAsync(x => x.RideBookingId == rideBookingId);
				if (review == null)
				{
					_logger.LogWarning("GetReviewByRideBookingId: no review found for ride booking {RideBookingId}", rideBookingId);
					return Result<GetReviewsDto>.Failure("No review found for this ride booking");
				}

				return Result<GetReviewsDto>.Success(MapToGetReviewsDto(review));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while getting review by ride booking id {RideBookingId}", rideBookingId);
				return Result<GetReviewsDto>.Failure("Unexpected error happened while getting review by ride booking id");
			}
		}
		public async Task<Result> UpdateReview(int reviewId, UpdateReviewDto model)
		{
			if (model == null)
			{
				_logger.LogWarning("UpdateReview called with null model for review id {ReviewId}", reviewId);
				return Result.Failure("Review data is required");
			}

			if (reviewId <= 0)
			{
				_logger.LogWarning("UpdateReview called with invalid review id {ReviewId}", reviewId);
				return Result.Failure("Valid ReviewId is required");
			}
			//if (model.Stars < 0.5f || model.Stars > 5f)
			//{
			//	_logger.LogWarning("UpdateReview called with invalid stars value {Stars} for review id {ReviewId}", model.Stars, reviewId);
			//	return Result.Failure("Stars must be between 0.5 and 5");
			//}
			try
			{
				var review = await _unitOfWork.Reviews.FirstOrDefaultAsync(x => x.Id == reviewId);
				if (review == null)
				{
					_logger.LogWarning("UpdateReview: review {ReviewId} not found", reviewId);
					return Result.Failure("Review not found");
				}

				var driverProfile = await _unitOfWork.DriverProfiles.FirstOrDefaultAsync(x => x.Id == review.DriverProfileId);
				if (driverProfile == null)
				{
					_logger.LogWarning("UpdateReview: driver profile {DriverProfileId} not found", review.DriverProfileId);
					return Result.Failure("Driver profile not found");
				}

				var oldStars = review.Stars;
				review.TextReview = string.IsNullOrWhiteSpace(model.TextReview) ? null : model.TextReview.Trim();
				review.Stars = model.Stars;
				review.UpdatedAt = DateTime.UtcNow;
				driverProfile.RatingSum = Math.Max(0f, driverProfile.RatingSum - oldStars + model.Stars);
				driverProfile.UpdatedAt = DateTime.UtcNow;

				_unitOfWork.Reviews.Update(review);
				_unitOfWork.DriverProfiles.Update(driverProfile);
				await _unitOfWork.SaveChangesAsync();

				return Result.Success("Review updated successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while updating review {ReviewId}", reviewId);
				return Result.Failure("Unexpected error happened while updating review");
			}
		}
		private static GetReviewsDto MapToGetReviewsDto(Review review)
		{
			return new GetReviewsDto
			{
				Id = review.Id,
				TextReview = review.TextReview,
				Stars = review.Stars,
				CreatedAt = review.CreatedAt,
				UpdatedAt = review.UpdatedAt
			};
		}
	}
}
