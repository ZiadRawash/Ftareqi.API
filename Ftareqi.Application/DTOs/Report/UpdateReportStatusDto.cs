using Ftareqi.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ftareqi.Application.DTOs.Report
{
	public class UpdateReportStatusDto
	{
		[Required(ErrorMessage = "Status is required.")]
		public ReportStatus Status { get; set; }
	}
}