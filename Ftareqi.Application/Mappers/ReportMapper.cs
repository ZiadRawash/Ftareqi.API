using Ftareqi.Application.DTOs.Report;
using Ftareqi.Domain.Models;

namespace Ftareqi.Application.Mappers
{
	public static class ReportMapper
	{
		public static GetReportDto ToDto(this Report report)
		{
			return new GetReportDto
			{
				Id = report.Id,
				ReporterUserId = report.ReporterUserId,
				ReporterName = report.ReporterUser?.FullName ?? string.Empty,
				ReportedUserId = report.ReportedUserId,
				ReportedUserName = report.ReportedUser?.FullName ?? string.Empty,
				Type = report.Type,
				Status = report.Status,
				Description = report.Description,
				CreatedAt = report.CreatedAt,
				UpdatedAt = report.UpdatedAt
			};
		}
	}
}