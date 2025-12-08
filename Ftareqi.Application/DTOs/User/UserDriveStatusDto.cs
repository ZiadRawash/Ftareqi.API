using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.User
{
	public class UserDriveStatusDto
	{
		public string? Id { get; set; }
		public string? FullName { get; set; }
		public string?PhoneNumber { get; set; }
		public DateTime CreatedAt { get; set; }
		public string? DriverStatus {  get; set; }
	}
}
