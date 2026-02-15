using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Paymob;
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
		Task<Result<PaymentResponseDto>> InitiateCardPaymentAsync(PaymentCardRequestDto requestData );

		Task<Result<PaymentResponseDto>> InitiateWalletPaymentAsync(PaymentWalletRequestDto requestData);

		bool VerifyHmac(string receivedHmac, PaymobCallback payload);
	}
	
}
