using DripOut.Application.Common.Settings;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.Common.Settings;
using Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.DTOs.Paymob.Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

public class PaymobPaymentGateway : IPaymentGateway
{
	private readonly PaymobSettings _paymobSettings;
	private readonly HttpClient _httpClient;
	private readonly ILogger<PaymobPaymentGateway> _logger;

	public PaymobPaymentGateway(HttpClient httpClient, IOptions<PaymobSettings> options, ILogger<PaymobPaymentGateway> logger)
	{
		_httpClient = httpClient;
		_paymobSettings = options.Value;
		_logger = logger;
	}

	public async Task<PaymentInitiationResult> InitiateCardPaymentAsync(PaymentCardRequestDto requestData)
	{
		_logger.LogInformation("Initiating card payment. Ref: {Reference}, Amount: {Amount}",
			requestData.Reference, requestData.Amount);

		try
		{
			var (paymentToken, paymobOrderId) = await PreparePaymentFlow(
				requestData.Amount,
				requestData.Reference,
				requestData.UserId,
				_paymobSettings.CardIntegrationId!);

			var iframeUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_paymobSettings.IframeId}?payment_token={paymentToken}";

			_logger.LogInformation("Card payment initiated successfully. Ref: {Reference}, OrderId: {OrderId}",
				requestData.Reference, paymobOrderId);

			return new PaymentInitiationResult
			{
				Success = true,
				Reference = requestData.Reference,
				RedirectUrl = iframeUrl,
				PaymobOrderId = paymobOrderId,
				Status = "pending",
				Message = "Card payment link generated successfully."
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Card payment failed. Ref: {Ref}", requestData.Reference);

			return new PaymentInitiationResult
			{
				Success = false,
				Reference = requestData.Reference,
				Message = ex.Message,
				Status = "failed"
			};
		}
	}

	public async Task<PaymentInitiationResult> InitiateWalletPaymentAsync(PaymentWalletRequestDto requestData)
	{
		_logger.LogInformation("Initiating wallet payment. Ref: {Reference}, Amount: {Amount}, Wallet: {WalletNumber}",
			requestData.reference, requestData.Amount, requestData.WalletNumber);

		try
		{
			var (paymentToken, paymobOrderId) = await PreparePaymentFlow(
				requestData.Amount,
				requestData.reference,
				requestData.UserId,
				_paymobSettings.WalletIntegrationId!);

			var response = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/acceptance/payments/pay", new
			{
				source = new { identifier = requestData.WalletNumber, subtype = "WALLET" },
				payment_token = paymentToken
			});

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Wallet payment rejected. Ref: {Reference}, StatusCode: {StatusCode}",
					requestData.reference, response.StatusCode);

				return new PaymentInitiationResult
				{
					Success = false,
					Reference = requestData.reference,
					Message = $"Payment request rejected. Status: {response.StatusCode}",
					Status = "failed"
				};
			}

			var result = await response.Content.ReadFromJsonAsync<PaymobWalletRedirectionResponse>();

			if (result == null || string.IsNullOrEmpty(result.redirect_url))
			{
				_logger.LogError("Missing redirect URL. Ref: {Reference}", requestData.reference);

				return new PaymentInitiationResult
				{
					Success = false,
					Reference = requestData.reference,
					Message = "Failed to get redirect URL from Paymob",
					Status = "failed"
				};
			}

			_logger.LogInformation("Wallet payment initiated successfully. Ref: {Reference}, OrderId: {OrderId}",
				requestData.reference, paymobOrderId);

			return new PaymentInitiationResult
			{
				Success = true,
				Reference = requestData.reference,
				RedirectUrl = result.redirect_url,
				PaymobOrderId = paymobOrderId,
				Status = "pending",
				Message = "Wallet redirection URL generated successfully."
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Wallet payment failed. Ref: {Ref}", requestData.reference);

			return new PaymentInitiationResult
			{
				Success = false,
				Reference = requestData.reference,
				Message = ex.Message,
				Status = "failed"
			};
		}
	}

	public  Result<PaymentCallbackResultDto> Callback(string hmac, PaymobCallbackDto callback)
	{
		try
		{
			var serialized = JsonConvert.SerializeObject(callback, Formatting.Indented);
			_logger.LogInformation("Callback payload: {Payload}", serialized);
			_logger.LogInformation("Received HMAC: {Hmac}", hmac);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to serialize callback payload");
		}

		if (callback?.obj == null)
		{
			_logger.LogError("Invalid or empty callback payload");
			return Result<PaymentCallbackResultDto>.Failure("Invalid payload");

		}
		// Verify HMAC
		var isValid = VerifyHmac(hmac, callback.obj);
		if (!isValid)
		{
			_logger.LogWarning("HMAC verification failed for transaction {TransactionId}", callback.obj.id);
			return Result<PaymentCallbackResultDto>.Failure(" HMAC mismatch");
		}

		// Process result
		if (callback.obj.success)
		{
			_logger.LogInformation("Payment successful. OrderId: {OrderId}, MerchantId: {MerchantId}", callback.obj.order?.id, callback.obj.order?.merchant_id);
			return Result<PaymentCallbackResultDto>.Success(
				new PaymentCallbackResultDto
				{
					MerchantId = callback.obj.order?.merchant_id,
					OrderId = callback.obj.order?.id ?? callback.obj.id,
					AmountCents = callback.obj.amount_cents,
			});
		}
		else
		{
			_logger.LogInformation("Payment failed. OrderId: {OrderId}, MerchantId: {MerchantId}", callback.obj.order?.id, callback.obj.order?.merchant_id);
			return Result<PaymentCallbackResultDto>.Failure("Invalid payload");
		}
	}
	private bool VerifyHmac(string receivedHmac, PaymobTransactionDto payload)
	{
		try
		{
			string concatenatedData = string.Concat(
				payload.amount_cents,
				payload.created_at ?? string.Empty,
				payload.currency ?? string.Empty,
				payload.error_occured.ToString().ToLower(),
				payload.has_parent_transaction.ToString().ToLower(),
				payload.id,
				payload.integration_id,
				payload.is_3d_secure.ToString().ToLower(),
				payload.is_auth.ToString().ToLower(),
				payload.is_capture.ToString().ToLower(),
				payload.is_refunded.ToString().ToLower(),
				payload.is_standalone_payment.ToString().ToLower(),
				payload.is_voided.ToString().ToLower(),
				payload.order?.id.ToString() ?? string.Empty,
				payload.owner,
				payload.pending.ToString().ToLower(),
				payload.source_data?.pan ?? string.Empty,
				payload.source_data?.sub_type ?? string.Empty,
				payload.source_data?.type ?? string.Empty,
				payload.success.ToString().ToLower()
			);

			var keyBytes = Encoding.UTF8.GetBytes(_paymobSettings.HMAC ?? string.Empty);
			var messageBytes = Encoding.UTF8.GetBytes(concatenatedData);

			using var hmac = new HMACSHA512(keyBytes);
			var hash = hmac.ComputeHash(messageBytes);
			var calculatedHmac = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

			var isValid = calculatedHmac.Equals(receivedHmac ?? string.Empty, StringComparison.OrdinalIgnoreCase);

			_logger.LogInformation("HMAC verification result: {Result} for transaction {TransactionId}", isValid ? "VALID" : "INVALID", payload.id);

			return isValid;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "HMAC verification exception for transaction {TransactionId}", payload.id);
			return false;
		}
	}

	private async Task<(string paymentToken, int orderId)> PreparePaymentFlow(decimal amount, string reference, string userId, string integrationId)
	{
		try
		{
			// Step 1: Authentication
			var authResponse = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/auth/tokens",
				new { api_key = _paymobSettings.APIKey });

			if (!authResponse.IsSuccessStatusCode)
			{
				throw new Exception($"Authentication failed with status: {authResponse.StatusCode}");
			}

			var authData = await authResponse.Content.ReadFromJsonAsync<PaymobAuthResponse>();
			if (authData == null || string.IsNullOrEmpty(authData.token))
			{
				throw new Exception("Authentication response missing token");
			}

			// Step 2: Create Order
			var orderResponse = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/ecommerce/orders", new
			{
				auth_token = authData.token,
				amount_cents = (int)(amount * 100),
				currency = "EGP",
				merchant_order_id = reference,
				items = new List<object>()
			});

			if (!orderResponse.IsSuccessStatusCode)
			{
				var orderContent = await orderResponse.Content.ReadAsStringAsync();
				throw new Exception($"Order creation failed: {orderContent}");
			}

			var orderData = await orderResponse.Content.ReadFromJsonAsync<PaymobOrderResponse>();
			if (orderData == null || orderData.id == 0)
			{
				throw new Exception("Order response missing ID");
			}

			// Step 3: Generate Payment Key
			var keyResponse = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/acceptance/payment_keys", new
			{
				auth_token = authData.token,
				amount_cents = (int)(amount * 100),
				expiration = 3600,
				order_id = orderData.id,
				billing_data = new
				{
					first_name = userId,
					last_name = "NA",
					email = "test@test.com",
					phone_number = "NA",
					city = "Cairo",
					country = "EG",
					apartment = "NA",
					floor = "NA",
					street = "NA",
					building = "NA"
				},
				currency = "EGP",
				integration_id = int.Parse(integrationId)
			});

			if (!keyResponse.IsSuccessStatusCode)
			{
				throw new Exception($"Payment key generation failed with status: {keyResponse.StatusCode}");
			}

			var keyData = await keyResponse.Content.ReadFromJsonAsync<PaymobKeyResponse>();
			if (keyData == null || string.IsNullOrEmpty(keyData.token))
			{
				throw new Exception("Payment key response missing token");
			}

			return (keyData.token, orderData.id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "PreparePaymentFlow failed. Ref: {Reference}", reference);
			throw;
		}
	}

	private class PaymobAuthResponse { public string? token { get; set; } }
	private class PaymobOrderResponse { public int id { get; set; } }
	private class PaymobKeyResponse { public string? token { get; set; } }
	private class PaymobWalletRedirectionResponse { public string? redirect_url { get; set; } }
}