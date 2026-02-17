using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.DTOs.Paymob.Ftareqi.Application.DTOs.Paymob;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IPaymentGateway
	{
		Task<PaymentInitiationResult> InitiateCardPaymentAsync(PaymentCardRequestDto requestData );

		Task<PaymentInitiationResult> InitiateWalletPaymentAsync(PaymentWalletRequestDto requestData);

		Result<PaymentCallbackResultDto> Callback(string hmac, PaymobCallbackDto callback);
	}
	
}
