using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class PhoneNumberRequestDto
	{
		[Required(ErrorMessage = "Phone number is required.")]
		public required string PhoneNumber { get; set; }
	}
}
