using Ftareqi.Application.Common.Consts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Common
{
	public class GenericQueryReq
	{
		[Range(1, int.MaxValue)]
		public int Page { get; set; } = 1;

		[Range(1, 100)]
		public int PageSize { get; set; } = 10;

		public bool SortDescending { get; set; } = false;
	}
}
