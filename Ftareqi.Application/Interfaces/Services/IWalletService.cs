using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs;
using Ftareqi.Application.DTOs.Paymob;
using Ftareqi.Domain.Enums.PaymentEnums;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IWalletService
	{
		public Task<Result<WalletResDto>> GetWallet(string userId);
		public Task<Result<PaginatedResponse<TransactionDto>>> GetWalletTransactionsPaginated(string userId, GenericQueryReq queryReq);
		public Task CreateWalletAsync(string userId);
		Task<Result<WalletTransaction>> LockAmountAsync(string userId, decimal amount);
		Task<Result> ReleaseLockedAmountAsync(string userId, decimal amount);
		Task<Result<PaymentResponseDto>> RecordPendingTopUpAsync(string userId, decimal amount, PaymentMethod method, string reference);
		Task<Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)>> CreditWalletAsync(string merchantReference);
		Task<Result<(string userId, WalletTransaction walletTrnx, PaymentTransaction paymentTrnx)>> FailWalletTransactionAsync(string merchantReference);
	}
}
