using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Paymob
{
	public class PaymentCardRequestDto
	{
		public required string  UserId { get; set; }
		public decimal Amount {  get; set; }
		public required string Reference { get; set; }

	}
}
