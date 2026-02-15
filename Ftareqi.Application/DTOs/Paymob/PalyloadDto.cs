using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Paymob
{
	public class PaymobCallback
	{
		public long id { get; set; } 
		public bool pending { get; set; }
		public long amount_cents { get; set; }
		public bool success { get; set; }
		public bool is_auth { get; set; }
		public bool is_capture { get; set; }
		public bool is_standalone_payment { get; set; }
		public bool is_voided { get; set; }
		public bool is_refunded { get; set; }
		public bool is_3d_secure { get; set; }
		public int integration_id { get; set; }
		public bool has_parent_transaction { get; set; }
		public CallbackOrder order { get; set; } 
		public string? created_at { get; set; }
		public string? currency { get; set; }
		public CallbackSourceData source_data { get; set; }
		public bool error_occured { get; set; }
		public int owner { get; set; }
	}

	public class CallbackOrder
	{
		public long id { get; set; } 

		// To Search Db for
		[JsonProperty("merchant_order_id")]
		public string merchant_order_id { get; set; }
	}

	public class CallbackSourceData
	{
		public string? type { get; set; }
		public string? pan { get; set; } 
		public string? sub_type { get; set; }
	}
}
