using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.LiveTracking
{
	public class DriveLocationResultDto
	{
		public bool IsSuccess { get; set; }
		public string? Message { get; set; }
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
	}
}
