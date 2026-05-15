using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.DTOs.Paymob.Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Enums.PaymentEnums;
using Ftareqi.Domain.Models;
using Ftareqi.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Ftareqi.Application.Orchestrators
{
	public class WalletOrchestrator : IWalletOrchestrator
	{
		private readonly IWalletService _walletService;
		private readonly IPaymentGateway _paymentGateway;
		private readonly IDistributedCachingService _cache;
		private readonly INotificationOrchestrator _notificationOrchestrator;
		private readonly ILogger<WalletOrchestrator> _logger;

		public WalletOrchestrator(
			IWalletService walletService,
			IPaymentGateway paymentGateway,
			IDistributedCachingService cache,
			INotificationOrchestrator notificationOrchestrator,
			ILogger<WalletOrchestrator> logger)
		{
			_walletService = walletService;
			_paymentGateway = paymentGateway;
			_cache = cache;
			_notificationOrchestrator = notificationOrchestrator;
			_logger = logger;
		}

		public async Task<Result<PaymentResponseDto>> TopUpWithCardAsync(string userId, TopUpWithCardReqDto model)
		{
			if (model == null)
				return Result<PaymentResponseDto>.Failure("Top up data is required");

			PaymentInitiationResult initiation;
			try
			{
				initiation = await _paymentGateway.InitiateCardPaymentAsync(new PaymentCardRequestDto
				{
					Amount = model.Amount,
					UserId = userId,
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error while calling card payment gateway for user {UserId}", userId);
				return Result<PaymentResponseDto>.Failure("Failed to initiate payment");
			}

			if (initiation == null || !initiation.Success)
				return Result<PaymentResponseDto>.Failure(initiation?.Message ?? "Payment initiation failed");

			var pendingResult = await _walletService.RecordPendingTopUpAsync(userId, model.Amount, PaymentMethod.Card, initiation.Reference);
			if (pendingResult.IsFailure)
				return Result<PaymentResponseDto>.Failure(pendingResult.Message);

			await _cache.RemoveWalletCachesAsync(userId);
			return Result<PaymentResponseDto>.Success(new PaymentResponseDto { PaymentUrl = initiation.RedirectUrl ?? string.Empty }, "Payment initiated successfully");
		}

		public async Task<Result<PaymentResponseDto>> TopUpWithWalletAsync(string userId, TopUpWithWalletReqDto model)
		{
			if (model == null)
				return Result<PaymentResponseDto>.Failure("Top up data is required");

			PaymentInitiationResult initiation;
			try
			{
				initiation = await _paymentGateway.InitiateWalletPaymentAsync(new PaymentWalletRequestDto
				{
					Amount = model.Amount,
					UserId = userId,
					WalletNumber = model.WalletNumber,
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error while calling wallet payment gateway for user {UserId}", userId);
				return Result<PaymentResponseDto>.Failure("Failed to initiate payment");
			}

			if (initiation == null || !initiation.Success)
				return Result<PaymentResponseDto>.Failure(initiation?.Message ?? "Payment initiation failed");

			var pendingResult = await _walletService.RecordPendingTopUpAsync(userId, model.Amount, PaymentMethod.MobileWallet, initiation.Reference);
			if (pendingResult.IsFailure)
				return Result<PaymentResponseDto>.Failure(pendingResult.Message);

			await _cache.RemoveWalletCachesAsync(userId);
			return Result<PaymentResponseDto>.Success(new PaymentResponseDto { PaymentUrl = initiation.RedirectUrl ?? string.Empty }, "Payment initiated successfully");
		}

		public async Task HandleCallbackAsync(string hmac, PaymobCallbackDto callback)
		{
			var callbackResult = _paymentGateway.Callback(hmac, callback);
			if (callbackResult.IsFailure)
			{
				_logger.LogWarning("Callback rejected: HMAC invalid or empty payload. No records updated.");
				return;
			}

			if (string.IsNullOrWhiteSpace(callbackResult.Data?.MerchantId))
			{
				_logger.LogWarning("Callback did not include a merchant reference. No records updated.");
				return;
			}

			Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)> walletResult;
			if (callbackResult.Data!.PaymentSucceeded)
				walletResult = await _walletService.CreditWalletAsync(callbackResult.Data.MerchantId);
			else
				walletResult = await _walletService.FailWalletTransactionAsync(callbackResult.Data.MerchantId);

			if (walletResult.IsFailure)
			{
				_logger.LogWarning("Callback processing skipped notification/cache update for reference {Reference}: {Message}", callbackResult.Data.MerchantId, walletResult.Message);
				return;
			}

			var metadata = new WalletTransactionMetadata
			{
				Preview = callbackResult.Data.PaymentSucceeded ? "Wallet Charged Successfully" : "Wallet charge failed",
				Amount = walletResult.Data.paymentTrnx.Amount,
				Type = walletResult.Data.walletTrnx.Type,
			};

			var notification = new NotificationInput(
				walletResult.Data.userId,
				NotificationCategory.Wallet,
				NotificationEventCode.WalletCharged,
				walletResult.Data.walletTrnx.UserWalletId.ToString(),
				metadata);

			await _notificationOrchestrator.NotifyAsync(notification);
			await _cache.RemoveWalletCachesAsync(walletResult.Data.userId);
		}

	}
}
