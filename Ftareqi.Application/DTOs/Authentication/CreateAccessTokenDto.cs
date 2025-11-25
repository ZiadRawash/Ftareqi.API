using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Authentication
{
	public class CreateAccessTokenDto
	{
		public string UserId { get; init; }= string.Empty;
		public IEnumerable<string> Roles { get; init; } = Enumerable.Empty<string>();
		public Dictionary<string, string> AdditionalClaims { get; init; }= new Dictionary<string, string>();
	}
}
