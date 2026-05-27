using Ftareqi.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ftareqi.Application.DTOs.Report
{
	public class CreateReportDto
	{
		[Required(ErrorMessage = "Reported user id is required.")]
		public string ReportedUserId { get; set; } = string.Empty;

		[Required(ErrorMessage = "Report type is required.")]
		public ReportReason Type { get; set; }

		[StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
		public string? Description { get; set; }
	}
}