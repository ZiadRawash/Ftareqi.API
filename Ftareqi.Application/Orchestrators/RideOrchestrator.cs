using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Bookings;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Orchestrators
{
	public class RideOrchestrator : IRideOrchestrator
	{
		private readonly ILogger<RideOrchestrator> _logger;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IRideService _rideService;
		private readonly IWalletService _walletService;
		private readonly IBookingService _bookingService;
		public RideOrchestrator(ILogger<RideOrchestrator> logger, IUnitOfWork unitOfWork,
			IRideService rideService, IWalletService walletService,
			 IBookingService bookingService)
		{
			_logger = logger;
			_unitOfWork = unitOfWork;
			_rideService = rideService;
			_walletService = walletService;
			_bookingService = bookingService;

		}
		// create rideBookingRequest
		//1-validateMoney 2- validate the bookingParam against the ride 3- look money 3- create request 4-send notifications of looking money and the request for driver
		public async Task<Result> CreateRideBookingRequest(CreateBookingRequestDto model, string userId)
		{
			var userFound = await _unitOfWork.Users.GetByIdAsync(userId);
			if (userFound == null)
			{
				_logger.LogError("user with {id} not found", userId);
				return Result.Failure("User not found");
			}
			var rideFound = await _unitOfWork.Rides.FirstOrDefaultAsNoTrackingAsync(x => x.Id == model.RideId);
			if (rideFound == null)
			{
				_logger.LogError("no such ride with id :{id}", model.RideId);
				return Result.Failure("ride not found");
			}
			var totalMoney = rideFound.PricePerSeat * model.NumberOfSeats;
			var moneyLocked = await _walletService.LockAmountAsync(userId, totalMoney);
			if (moneyLocked.IsFailure )
			{
				return Result.Failure(moneyLocked.Message);
			}
			var reservedId =await _bookingService.CreateBooking(model, userId);
			if (reservedId.IsFailure)
			{
				var releaseResult = await _walletService.ReleaseLockedAmountAsync(userId, totalMoney);
				if (releaseResult.IsFailure)
				{
					_logger.LogError("Failed to release locked amount for user {UserId} after booking create failure. BookingError: {BookingError}, ReleaseError: {ReleaseError}", userId, reservedId.Message, releaseResult.Message);
					return Result.Failure($"{reservedId.Message}. Also failed to release locked amount, please contact support.");
				}

				return Result.Failure(reservedId.Message);
			}

			var userWallet = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == userId);
			if (userWallet == null)
			{
				_logger.LogError("Failed to find wallet for user {UserId} after successful booking creation {BookingId}", userId, reservedId.Data);
				return Result.Failure("Booking created but failed to link wallet transaction to booking");
			}

			var lockTransactions = await _unitOfWork.WalletTransactions.FindAllAsTrackingAsync(
				x => x.UserWalletId == userWallet.Id
					 && x.Type == TransactionType.locked
					 && x.Amount == totalMoney
					 && x.RideBookingId == null);

			var lockTransaction = lockTransactions
				.OrderByDescending(x => x.CreatedAt)
				.ThenByDescending(x => x.Id)
				.FirstOrDefault();

			if (lockTransaction == null)
			{
				_logger.LogError("Failed to find lock transaction for user {UserId} and booking {BookingId}", userId, reservedId.Data);
				return Result.Failure("Booking created but failed to link wallet transaction to booking");
			}

			lockTransaction.RideBookingId = reservedId.Data;
			lockTransaction.UpdatedAt = DateTime.UtcNow;
			_unitOfWork.WalletTransactions.Update(lockTransaction);
			await _unitOfWork.SaveChangesAsync();

			return Result.Success("Booking Created successfully");
		}
	}
}