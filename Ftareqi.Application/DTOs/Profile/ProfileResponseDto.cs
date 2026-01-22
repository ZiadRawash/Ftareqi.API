using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.User
{
	public class ProfileResponseDto
	{
		public string? Id { get; set; }
		public string? FullName { get; set; }
		public Gender Gender { get; set; }
		public DateTime CreatedAt { get; set; }
		public string? UserImage { get; set; }
		public string? PhoneNumber { get; set; }
		public bool IsDriver {  get; set; }
		public bool PhoneNumberConfirmed { get; set; }
		public int? DriverId { get; set; }
		
	}
}
