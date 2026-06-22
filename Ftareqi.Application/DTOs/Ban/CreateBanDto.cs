using Ftareqi.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ftareqi.Application.DTOs.Ban
{
	public class CreateBanDto
	{
		[Required(ErrorMessage = "Ban type is required.")]
		public ReportReason? Type { get; set; }

		[Range(1, int.MaxValue, ErrorMessage = "Ban days must be greater than zero.")]
		public int Days { get; set; }

		[StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
		public string? Description { get; set; }
	}
}
