using Ftareqi.Domain.Enums.PaymentEnums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class PaymentTransaction
	{
		public int Id { get; set; }
		public decimal Amount { get; set; }
		public PaymentType PaymentType { get; set; }//Credit,Debit
		public PaymentMethod method { get; set; }//Card,MobileWallet
		public PaymentStatus Status { get; set; }//Success,Failed,Pending
		public string Reference { get; set; }
		public DateTime CreatedAt { get; set; }

		public User User { get; set; }
		[ForeignKey(nameof(User))]
		public string UserId { get; set; }
	}
}
