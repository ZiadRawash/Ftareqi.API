using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs;
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
	}
}
