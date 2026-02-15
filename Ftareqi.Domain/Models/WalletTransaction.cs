using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class WalletTransaction
	{
		public int Id { get; set; }
		public TransactionType Type { get; set; }//Deposit,Withdrawal,RidePayment,Earnings,Refund
		public decimal Amount { get; set; }
		public decimal BalanceBefore { get; set; }
		public decimal BalanceAfter { get; set; }
		public  TransactionStatus Status { get; set; }//Pending,Completed,Failed
		public DateTime CreatedAt {  get; set; }
		public DateTime UpdatedAt {  get; set; }
		public UserWallet UserWallet { get; set; }
		[ForeignKey(nameof(UserWallet))]
		public int UserWalletId {  get; set; }
	}
}
