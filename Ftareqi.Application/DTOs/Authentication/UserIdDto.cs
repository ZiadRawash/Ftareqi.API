using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class UserIdDto
	{
		[Required]
		public string? Id { get; set; } 
	}
}
