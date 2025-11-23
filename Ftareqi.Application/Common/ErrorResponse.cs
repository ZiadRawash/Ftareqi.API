using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Common
{
	public class ErrorResponse
	{
		public string? Type { get; set; }

		public string? Title { get; set; }

		public int Status { get; set; }

		public string? Detail { get; set; }
	}
}
