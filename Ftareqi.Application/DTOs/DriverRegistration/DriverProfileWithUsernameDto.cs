using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.DriverRegistration
{
	public class DriverProfileWithUsernameDto
	{
		public string? FullName { get; set; }
		public string? PhoneNumber { get; set; }
		public  DateTime CreatedAt { get; set; }
	}
}
