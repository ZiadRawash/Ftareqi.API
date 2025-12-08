using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.User
{
	public class UserWithRolesDto
	{
		public string? Id { get; set; }
		public string? FullName { get; set; }
		public  string? PhoneNumber { get; set; }
		public  string? Image { get; set; }
		public IEnumerable<string>Roles { get; set; }= Enumerable.Empty<string>();
	}
}
