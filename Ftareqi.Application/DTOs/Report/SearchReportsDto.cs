using Ftareqi.Application.Common;
using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Report
{
	public class SearchReportsDto:GenericQueryReq
	{
		public ReportReason? Reason { get; set; }
		public ReportStatus? Status { get; set; }
	}
}
