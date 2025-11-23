using Ftareqi.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Ftareqi.Persistence.Repositories
{
	public class BaseRepository<T> : IBaseRepository<T> where T : class
	{
		private readonly ApplicationDbContext _context;
		private readonly DbSet<T> _dbSet;

		public BaseRepository(ApplicationDbContext dbContext)
		{
			_context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			_dbSet = dbContext.Set<T>();
		}
		public async Task AddAsync(T entity)
		{
			await _dbSet.AddAsync(entity);
		}
		public async Task AddRangeAsync(IEnumerable<T> entities)
		{
			await _dbSet.AddRangeAsync(entities);
		}

		public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
		{
			return await _dbSet.CountAsync(predicate);
		}

		public void Delete(T entity)
		{
			_dbSet.Remove(entity);
		}

		public void DeleteRange(IEnumerable<T> entities)
		{
			_dbSet.RemoveRange(entities);
		}

		public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
		{
			return await _dbSet.AnyAsync(predicate);
		}

		public async Task<IEnumerable<T>> FindAllAsNoTrackingAsync(Expression<Func<T, bool>> predicate)
		{
			return await _dbSet
				.AsNoTracking()
				.Where(predicate)
				.ToListAsync();
		}
		public async Task<IEnumerable<T>> FindAllAsNoTrackingAsync(
			Expression<Func<T, bool>> predicate,
			params Expression<Func<T, object>>[] includes)
		{
			IQueryable<T> query = _dbSet.AsNoTracking().Where(predicate);

			if (includes != null && includes.Any())
			{
				query = includes.Aggregate(query, (current, include) => current.Include(include));
			}

			return await query.ToListAsync();
		}

		public async Task<IEnumerable<T>> FindAllAsTrackingAsync(Expression<Func<T, bool>> predicate)
		{
			return await _dbSet.Where(predicate).ToListAsync();
		}

		public async Task<IEnumerable<T>> FindAllAsTrackingAsync(
			Expression<Func<T, bool>> predicate,
			params Expression<Func<T, object>>[] includes)
		{
			IQueryable<T> query = _dbSet.Where(predicate);

			if (includes != null && includes.Any())
			{
				query = includes.Aggregate(query, (current, include) => current.Include(include));
			}

			return await query.ToListAsync();
		}

		public async Task<IEnumerable<T>> GetAllAsNoTrackingAsync()
		{
			return await _dbSet.AsNoTracking().ToListAsync();
		}

		public async Task<IEnumerable<T>> GetAllAsTrackingAsync()
		{
			return await _dbSet.ToListAsync();
		}

		public async Task<T?> GetByIdAsync(params object[] keyValues)
		{
			return await _dbSet.FindAsync(keyValues);
		}

		public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
			int pageNumber,
			int pageSize,
			Expression<Func<T, bool>>? predicate = null,
			params Expression<Func<T, object>>[] includes)
		{
			IQueryable<T> query = _dbSet;

			if (includes != null && includes.Any())
			{
				query = includes.Aggregate(query, (current, include) => current.Include(include));
			}

			if (predicate != null)
			{
				query = query.Where(predicate);
			}

			int totalCount = await query.CountAsync();

			var items = await query
				.AsNoTracking()
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (items, totalCount);
		}

		public void Update(T entity)
		{
			_dbSet.Update(entity);
		}

		public void UpdateRange(IEnumerable<T> entities)
		{
			_dbSet.UpdateRange(entities);
		}
	}
}
