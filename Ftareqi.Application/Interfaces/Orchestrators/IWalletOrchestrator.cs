using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.DTOs.Paymob.Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Orchestrators
{
	public interface IWalletOrchestrator
	{
		Task<Result<PaymentResponseDto>> TopUpWithCardAsync(string userId, TopUpWithCardReqDto model);
		Task<Result<PaymentResponseDto>> TopUpWithWalletAsync(string userId, TopUpWithWalletReqDto model);
		Task HandleCallbackAsync(string hmac, PaymobCallbackDto callback);
	}
}
