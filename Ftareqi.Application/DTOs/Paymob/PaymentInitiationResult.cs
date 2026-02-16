using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Paymob
{
	public class PaymentInitiationResult
	{
		public bool Success { get; set; }
		public string Reference { get; set; }      // transaction id أو payment intent
		public string RedirectUrl { get; set; }    // لو محتاج إعادة توجيه
		public string Message { get; set; }
		public int PaymobOrderId { get; set; }
		public string Status { get; set; } = string.Empty;
	}
}
