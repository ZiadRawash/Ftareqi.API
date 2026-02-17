using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs;
using Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.DTOs.Paymob.Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Ftareqi.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class WalletController : ControllerBase
	{
		private readonly ILogger<WalletController> _logger;

		private readonly IWalletService _walletService;
		private readonly IPaymentGateway _paymentGateway;

		public WalletController(
			ILogger<WalletController> logger,
			IWalletService walletService,
			IPaymentGateway paymentGateway)
		{
			_walletService = walletService;
			_paymentGateway = paymentGateway;
			_logger = logger;
		}
		[HttpGet()]
		[Authorize]
		public async Task<ActionResult<ApiResponse<WalletResDto>>> GetWallet()
		{
			var userId = User.GetUserId();
			var wallet = await _walletService.GetWallet(userId);
			if (wallet.IsSuccess)
			{
				return Ok(new ApiResponse<WalletResDto>
				{
					Errors = wallet.Errors,
					Message = wallet.Message,
					Success = wallet.IsSuccess,
					Data = wallet.Data

				});
			}
			return BadRequest(new ApiResponse
			{
				Errors = wallet.Errors,
				Message = wallet.Message,
				Success = wallet.IsSuccess,
			});
		}

		[HttpGet("transactions")]
		[Authorize]
		public async Task<ActionResult<ApiResponse<WalletTransactionDto>>> WalletTransactions()
		{
			var userId = User.GetUserId();
			var transactions = await _walletService.GetWalletTransactions(userId);
			if (transactions.IsSuccess) {
				return Ok(new ApiResponse<WalletTransactionDto>
				{
					Errors = transactions.Errors,
					Message = transactions.Message,
					Success = transactions.IsSuccess,
					Data = transactions.Data

				});
			}
			return BadRequest(new ApiResponse
			{
				Errors = transactions.Errors,
				Message = transactions.Message,
				Success = transactions.IsSuccess,
			});
		}

		[Authorize]
		[HttpPost("top-up/mobile-wallet")]

		public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> TopUpWithWallet(TopUpWithWalletReqDto model)
		{
			var userId = User.GetUserId();
			if (!ModelState.IsValid || userId==null)
			{
				return BadRequest(new ApiResponse
				{
					Errors = ["Validation errors"],
					Message = "Validation errors",
					Success = false,
				});
			}
			
			var result = await _walletService.TopUpWithWalletAsync(
				userId,
				model.Amount,
				() => _paymentGateway.InitiateWalletPaymentAsync(
					new PaymentWalletRequestDto
					{
						Amount = model.Amount,
						UserId = userId,
						WalletNumber = model.WalletNumber,
					})
				);
			if (!result.IsSuccess){
				return BadRequest(new ApiResponse
				{
					Errors = result.Errors,
					Message = result.Message,
					Success = result.IsSuccess,
				});
			}
			return Ok(new ApiResponse<PaymentResponseDto>
			{
				Errors = result.Errors,
				Message = result.Message,
				Data = result.Data,
				Success = result.IsSuccess,
			});
		}


		[Authorize]
		[HttpPost("top-up/card")]
		public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> TopUpWithCard(TopUpWithCardReqDto model)
		{
			var userId = User.GetUserId();
			if (!ModelState.IsValid || userId == null)
			{
				return BadRequest(new ApiResponse
				{
					Errors = ["Validation errors"],
					Message = "Validation errors",
					Success = false,
				});
			}
			var result = await _walletService.TopUpWithCardAsync(
				userId,
				model.Amount,
				() => _paymentGateway.InitiateCardPaymentAsync(
					new PaymentCardRequestDto
					{
						Amount = model.Amount,
						UserId = userId,
					})
				);
			if (!result.IsSuccess)
			{
				return BadRequest(new ApiResponse
				{
					Errors = result.Errors,
					Message = result.Message,
					Success = result.IsSuccess,
				});
			}
			return Ok(new ApiResponse<PaymentResponseDto>
			{
				Errors = result.Errors,
				Message = result.Message,
				Data = result.Data,
				Success = result.IsSuccess,
			});
		}

		[HttpPost("callback")]
		public async Task<IActionResult> Callback([FromQuery] string hmac)
		{
			string rawBody;
			using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
				rawBody = await reader.ReadToEndAsync();

			_logger.LogInformation("Raw callback body: {Body}", rawBody);

			PaymobCallbackDto? callback;
			try
			{
				callback = JsonConvert.DeserializeObject<PaymobCallbackDto>(rawBody);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to deserialize callback body");
				return Ok();
			}

			if (callback == null)
			{
				_logger.LogError("Callback deserialized to null. Raw body was: {Body}", rawBody);
				return Ok();
			}

			await _walletService.ProcessPaymentCallBack(hmac, callback);
			return Ok();
		}

	}
}
