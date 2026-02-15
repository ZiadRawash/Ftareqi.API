using DripOut.Application.Common.Settings;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.Common.Settings;
using Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

	public async Task<Result<PaymentResponseDto>> InitiateCardPaymentAsync(PaymentCardRequestDto requestData)
	{
		try
		{
			var (paymentToken, paymobOrderId) = await PreparePaymentFlow(requestData.Amount, requestData.Reference, requestData.UserId, _paymobSettings.CardIntegrationId!);

			var iframeUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_paymobSettings.IframeId}?payment_token={paymentToken}";

			return Result<PaymentResponseDto>.Success(new PaymentResponseDto
			{
				PaymentUrl = iframeUrl,
				PaymobOrderId = paymobOrderId,
				Reference = requestData.Reference
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Card Payment Initiation Failed for Ref: {Ref}", requestData.Reference);
			return Result<PaymentResponseDto>.Failure(ex.Message);
		}
	}

	public async Task<Result<PaymentResponseDto>> InitiateWalletPaymentAsync(PaymentWalletRequestDto requestData)
	{
		try
		{
			var (paymentToken, paymobOrderId) = await PreparePaymentFlow(requestData.Amount, requestData.reference, requestData.UserId, _paymobSettings.WalletIntegrationId!);

			var response = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/acceptance/payments/pay", new
			{
				source = new { identifier = requestData.WalletNumber, subtype = "WALLET" },
				payment_token = paymentToken
			});

			if (!response.IsSuccessStatusCode)
				return Result<PaymentResponseDto>.Failure("Failed to get wallet redirection URL");

			var result = await response.Content.ReadFromJsonAsync<PaymobWalletRedirectionResponse>();

			return Result<PaymentResponseDto>.Success(new PaymentResponseDto
			{
				PaymentUrl = result!.redirect_url!, 
				PaymobOrderId = paymobOrderId,
				Reference = requestData.reference
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Wallet Payment Initiation Failed for Ref: {Ref}", requestData.reference);
			return Result<PaymentResponseDto>.Failure(ex.Message);
		}
	}

	public bool VerifyHmac(string receivedHmac, PaymobCallback payload)
	{
		string concatenatedData = string.Concat(
			payload.amount_cents,
			payload.created_at,
			payload.currency,
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
			payload.order.id,
			payload.owner,
			payload.pending.ToString().ToLower(),
			payload.source_data.pan ?? "",
			payload.source_data.sub_type,
			payload.source_data.type,
			payload.success.ToString().ToLower()
		);

		var keyBytes = Encoding.UTF8.GetBytes(_paymobSettings.HMAC!);
		var messageBytes = Encoding.UTF8.GetBytes(concatenatedData);

		using var hmac = new HMACSHA512(keyBytes);
		var hash = hmac.ComputeHash(messageBytes);
		var calculatedHmac = BitConverter.ToString(hash).Replace("-", "").ToLower();

		return calculatedHmac.Equals(receivedHmac, StringComparison.OrdinalIgnoreCase);
	}

	

	private async Task<(string token, int orderId)> PreparePaymentFlow(decimal amount, string reference, string userId,  string integrationId)
	{
		var authResponse = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/auth/tokens", new { api_key = _paymobSettings.APIKey });
		var authData = await authResponse.Content.ReadFromJsonAsync<PaymobAuthResponse>();

		var orderResponse = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/ecommerce/orders", new
		{
			auth_token = authData!.token,
			amount_cents = (int)(amount * 100),
			currency = "EGP",
			merchant_order_id = reference,
			items = new List<object>()
		});
		var orderData = await orderResponse.Content.ReadFromJsonAsync<PaymobOrderResponse>();

		// 3. Payment Key Generation
		var keyResponse = await _httpClient.PostAsJsonAsync("https://accept.paymob.com/api/acceptance/payment_keys", new
		{
			auth_token = authData.token,
			amount_cents = (int)(amount * 100),
			expiration = 3600,
			order_id = orderData!.id,
			billing_data = new
			{
				first_name = userId,
				last_name = "",
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
		var keyData = await keyResponse.Content.ReadFromJsonAsync<PaymobKeyResponse>();

		return (keyData!.token!, orderData.id);
	}


	private class PaymobAuthResponse { public string? token { get; set; } }
	private class PaymobOrderResponse { public int id { get; set; } }
	private class PaymobKeyResponse { public string? token { get; set; } }
	private class PaymobWalletRedirectionResponse { public string? redirect_url { get; set; } }
}