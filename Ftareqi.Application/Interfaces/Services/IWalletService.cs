using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs;
using Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Application.DTOs.Paymob.Ftareqi.Application.DTOs.Paymob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IWalletService
	{
		public Task<Result<WalletTransactionDto>> GetWalletTransactions(int walletId);
		public Task CreateWalletAsync(string userId);
		Task<Result<PaymentResponseDto>> TopUpWithCardAsync(
			string userId,
			decimal amount,
			Func<Task<PaymentInitiationResult>>  initiateCardPayment);

		Task<Result<PaymentResponseDto>> TopUpWithWalletAsync(
			string userId,
			decimal amount,
			Func<Task<PaymentInitiationResult>> initiateWalletPayment);

		Task ProcessPaymentCallBack(string hmac, PaymobCallbackDto callback);
	}
}
