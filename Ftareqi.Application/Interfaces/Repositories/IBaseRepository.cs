
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace Ftareqi.Application.Interfaces.Repositories
{
	public interface IBaseRepository<T> where T : class
	{
		//add
		Task AddAsync(T entity);
		Task AddRangeAsync(IEnumerable<T> entities);

		// Read
		Task<T?> GetByIdAsync(params object[] keyValues);
		Task<IEnumerable<T>> GetAllAsNoTrackingAsync();
		Task<IEnumerable<T>> GetAllAsTrackingAsync();
		Task<IEnumerable<T>> FindAllAsNoTrackingAsync(Expression<Func<T, bool>> predicate);
		Task<IEnumerable<T>> FindAllAsNoTrackingAsync(
			Expression<Func<T, bool>> predicate,
			params Expression<Func<T, object>>[] includes);

		Task<IEnumerable<T>> FindAllAsTrackingAsync(Expression<Func<T, bool>> predicate);
		Task<IEnumerable<T>> FindAllAsTrackingAsync(
			Expression<Func<T, bool>> predicate,
			params Expression<Func<T, object>>[] includes);

		Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
		Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
		Task<T?> FirstOrDefaultAsNoTrackingAsync(Expression<Func<T, bool>> predicate);

		 Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
			int pageNumber,
			int pageSize,
			Expression<Func<T, object>> orderBy,
			Expression<Func<T, bool>>? predicate = null,
			bool descending = false,
			params Expression<Func<T, object>>[] includes);

		// Helpers
		Task<int> CountAsync(Expression<Func<T, bool>> predicate);
		Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

		// Update
		void Update(T entity);
		void UpdateRange(IEnumerable<T> entities);

		// Delete
		void Delete(T entity);
		void DeleteRange(IEnumerable<T> entities);
	}
}