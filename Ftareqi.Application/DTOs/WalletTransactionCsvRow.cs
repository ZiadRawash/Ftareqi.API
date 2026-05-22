using System;

namespace Ftareqi.Application.DTOs
{
	public class WalletTransactionCsvRow
	{
		public int Id { get; set; }
		public string Type { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public decimal BalanceBefore { get; set; }
		public decimal BalanceAfter { get; set; }
		public string CreatedAt { get; set; } = string.Empty;
		public string? UpdatedAt { get; set; }
	}
}
