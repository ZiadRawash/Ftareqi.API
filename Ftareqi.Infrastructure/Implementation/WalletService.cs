using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Mappers;
using Ftareqi.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class WalletService : IWalletService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<WalletService> _logger;
		public WalletService(IUnitOfWork unitOfWork , ILogger<WalletService> logger)
		{
			_unitOfWork=unitOfWork;
			_logger=logger;
		}

		public async Task CreateWalletAsync(string userId)
		{
			var walletCreated = await _unitOfWork.UserWallets.FirstOrDefaultAsNoTrackingAsync(x => x.UserId == userId);
			if (walletCreated == null)
			{
				var CreateWallet = new UserWallet
				{
					UserId = userId,
					balance = 0,
					CreatedAt = DateTime.UtcNow,
					IsLocked = false,
					PendingBalance = 0,
				};
			await _unitOfWork.UserWallets.AddAsync(CreateWallet);
			 await _unitOfWork.SaveChangesAsync();
			}
		}

		public async Task<Result<WalletTransactionDto>> GetWalletTransactions(int walletId)
		{
			var transactions = await _unitOfWork.WalletTransactions.FindAllAsNoTrackingAsync(x=>x.UserWalletId == walletId);
			var result = WalletMapper.ToDto(walletId, transactions ?? Enumerable.Empty<WalletTransaction>());
			return Result<WalletTransactionDto>.Success(result);

		}

	}
}
