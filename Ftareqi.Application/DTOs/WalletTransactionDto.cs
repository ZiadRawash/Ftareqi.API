using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs
{
	public class WalletTransactionDto
	{
		public int UserWalletId { get; set; }
		public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
	}
}
