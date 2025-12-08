using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Common.Helpers
{
	public static class ExtensionMethods
	{
		public static IQueryable<T> WhereIf<T>(this IQueryable<T> Source, bool Condition, Expression<Func<T, bool>> predicate)
		=> Condition ? Source.Where(predicate) : Source;
	}
}
