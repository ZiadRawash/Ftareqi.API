using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Paymob
{
	public class PaymentResponseDto
	{

		public string PaymentUrl { get; set; }=string.Empty;
		public int PaymobOrderId { get; set; }    
		public string Reference { get; set; } = string.Empty;

	}
}
