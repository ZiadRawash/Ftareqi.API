using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class UserClaimDto
	{
		public string Type { get; set; } = string.Empty;
		public string Value { get; set; }=string.Empty;
	}
}
