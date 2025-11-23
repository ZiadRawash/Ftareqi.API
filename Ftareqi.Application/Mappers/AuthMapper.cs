using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Mappers
{
	public static class AuthMapper
	{
		public static UserDto MapTo(this User model)
		{
			return new UserDto
			{
				FullName = model.FullName,
				Gender = model.Gender,
				Id = model.Id,
				DateOfBirth = model.DateOfBirth!,
				PenaltyCount = model.PenaltyCount,
				PhoneNumber = model.PhoneNumber!,
				PhoneNumberConfirmed = model.PhoneNumberConfirmed,
			};
		}
	}
}
