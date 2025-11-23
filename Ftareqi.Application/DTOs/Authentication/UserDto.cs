using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class UserDto
	{
		public string Id { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string FullName { get; set; } = string.Empty;
		public Gender Gender { get; set; }
		public DateTime DateOfBirth { get; set; }
		public bool PhoneNumberConfirmed { get; set; }
		public int PenaltyCount { get; set; }
	}
}
