using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Notification
{
	public class WalletTransactionMetadata: NotificationMetadata
	{
		public TransactionType Type { get; set; }
		public decimal Amount { get; set; }

	}
}
