using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	using Ftareqi.Domain.Enums;
	using System.ComponentModel.DataAnnotations;

	public class RegisterRequestDto
	{
		public required string FullName { get; set; }

		public required string PhoneNumber { get; set; }

		public required string Password { get; set; }
		public Gender Gender { get; set; }
		public DateTime DateOfBirth { get; set; }

	}
}
