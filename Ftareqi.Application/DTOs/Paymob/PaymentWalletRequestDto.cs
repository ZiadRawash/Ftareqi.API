using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Paymob
{
	public class PaymentWalletRequestDto
	{
		public required string UserId { get; set; }
		public  decimal Amount { get; set; }
		public required string reference { get; set; }
		public required string WalletNumber { get; set; }
	}
}
