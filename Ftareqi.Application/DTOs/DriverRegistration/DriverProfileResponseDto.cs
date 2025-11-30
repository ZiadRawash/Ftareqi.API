using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.DriverRegistration
{
	public class DriverProfileResponseDto
	{
		public string UserId { get; set; } = string.Empty;
		public int DriverProfileId { get; set; }
		public string Status { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
	}
}
