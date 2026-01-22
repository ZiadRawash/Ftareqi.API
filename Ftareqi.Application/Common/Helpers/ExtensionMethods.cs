using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Common.Helpers
{
	public static class ExtensionMethods
	{
		public static IQueryable<T> WhereIf<T>(this IQueryable<T> Source, bool Condition, Expression<Func<T, bool>> predicate)
		=> Condition ? Source.Where(predicate) : Source;

		public static string GetUserId(this ClaimsPrincipal user)
		{
			var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (string.IsNullOrEmpty(userId))
				return null!;

			return userId;
		}
	}
}


