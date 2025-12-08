using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IUserClaimsService
	{

		public Task<Result<IEnumerable<string>>> GetUserRolesAsync(string userId);
		public Task<Result> AddClaimAsync(string userId, string claimType , string claimValue);
		public Task<Result> AddRolesAsync(string userId, IEnumerable<string>roles);
		public Task<Result<Dictionary<string, string>>> GetUserClaimsAsync(string userId);
		public Task<Result> RemoveClaimAsync (string userId , string claimType);
		public Task<Result> RemoveRoleAsync(string userId, string role);

	}
}
