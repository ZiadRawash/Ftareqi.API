using Ftareqi.Application.Common.Consts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs
{
	public class GenericQueryModel
	{
		[Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
		public int Page { get; set; } = 1;

		[Range(1, 100, ErrorMessage = "PageSize must be between 0 and 100.")]
		public int PageSize { get; set; }
		public bool SortDescending { get; set; } = false;
	}
}
