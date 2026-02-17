using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Paymob
{
	public class TopUpWithWalletReqDto
	{
		[Required]
			[Phone]
		[StringLength(15, MinimumLength = 10)]
		public required string WalletNumber { get; set; }

		[Required]
		[Range(1, 10000)]
		public decimal Amount { get; set; }
	}
}
