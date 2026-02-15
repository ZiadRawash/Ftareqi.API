using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Common.Settings
{
	public class PaymobSettings
	{
		public string? HMAC {  get; set; }
		public string? APIKey {  get; set; }
		public string? MerchantID {  get; set; }
		public string? CardIntegrationId { get; set; }
		public string? IframeId { get; set; }
		public string?WalletIntegrationId { get; set; }

	}
}
