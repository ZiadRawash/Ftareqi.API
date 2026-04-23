using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Common.Settings
{
	public class TwilioSettings
	{
		public string AccountSID { get; set; }=string.Empty;
		public string AuthToken { get; set; } = string.Empty;
		public string TwilioPhoneNumber { get; set; }=string.Empty;
	}
}
