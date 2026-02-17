using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Paymob
{
	namespace Ftareqi.Application.DTOs.Paymob
	{
		public class PaymobCallbackDto
		{
			public PaymobTransactionDto obj { get; set; } = new();
			public string? type { get; set; }
		}

		public class PaymobTransactionDto
		{
			public int id { get; set; }
			public bool pending { get; set; }
			public int amount_cents { get; set; }
			public bool success { get; set; }
			public bool is_auth { get; set; }
			public bool is_capture { get; set; }
			public bool is_voided { get; set; }
			public bool is_refunded { get; set; }
			public bool is_3d_secure { get; set; }
			public int integration_id { get; set; }
			public int profile_id { get; set; }
			public bool has_parent_transaction { get; set; }
			public PaymobOrderDto order { get; set; } = new();
			public string? created_at { get; set; }
			public string? currency { get; set; }
			public string? merchant_commission { get; set; }
			public bool is_standalone_payment { get; set; }
			public string? source_data_type { get; set; }
			public PaymobSourceDataDto source_data { get; set; } = new();
			public bool error_occured { get; set; }
			public int owner { get; set; }
		}

		public class PaymobOrderDto
		{
			public int id { get; set; }

			// This is YOUR reference — the guid you passed as merchant_order_id when creating the order
			[JsonProperty("merchant_order_id")]
			public string? merchant_order_id { get; set; }

			// Remove or keep merchant_id — it's Paymob's internal merchant account id, not your reference
			public int? merchant_id { get; set; }
		}

		public class PaymobSourceDataDto
		{
			public string? pan { get; set; }
			public string? sub_type { get; set; }
			public string? type { get; set; }
		}

		public class PaymentCallbackResultDto
		{
			/// <summary>
			/// Your internal merchant order reference stored in PaymentTransaction.Reference.
			/// </summary>
			public string? MerchantId { get; set; }

			public int OrderId { get; set; }

			/// <summary>Amount in cents as received from Paymob.</summary>
			public int AmountCents { get; set; }

			/// <summary>
			/// True  → Paymob confirmed the charge went through.
			/// False → Paymob reported a failure (card declined, insufficient funds, etc.).
			/// Populated for both success and PAYMENT_FAILED result codes.
			/// </summary>
			public bool PaymentSucceeded { get; set; }
		}
	}
}
