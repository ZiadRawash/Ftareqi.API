using Ftareqi.Application.DTOs;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Mappers
{
	public static class WalletMapper
	{
		public static WalletTransactionDto ToDto(
		int walletId,
		IEnumerable<WalletTransaction> transactions)
		{
			return new WalletTransactionDto
			{
				UserWalletId = walletId,
				Transactions = transactions
					.OrderByDescending(t => t.CreatedAt)
					.Select(t => new TransactionDto
					{
						Id = t.Id,
						Type = t.Type,
						Amount = t.Amount,
						BalanceBefore = t.BalanceBefore,
						BalanceAfter = t.BalanceAfter,
						Status = t.Status,
						CreatedAt = t.CreatedAt,
						UpdatedAt = t.UpdatedAt
					})
					.ToList()
			};
		}
	}
}
