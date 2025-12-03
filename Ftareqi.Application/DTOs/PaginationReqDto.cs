using Ftareqi.Application.Common.Consts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs
{
	public class PaginationReqDto
	{
		[Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
		[Required]
		public int Page { get; set; } = 1;

		[Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
		[Required]
		public int PageSize { get; set; } = 10;
		public bool SortDescending { get; set; } = false;
	}
}
