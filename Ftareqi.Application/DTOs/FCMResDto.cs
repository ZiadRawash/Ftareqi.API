using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs
{
	public class FcmSendResult
	{
		public List<string> SuccessTokens { get; set; } = new();
		public List<string> InvalidTokens { get; set; } = new();
		public List<string> FailedTokens { get; set; } = new();

		public bool AllSucceeded => FailedTokens.Count == 0 && InvalidTokens.Count == 0;
	}
}
