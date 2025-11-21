using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Persistence.Repositories
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly ApplicationDbContext _applicationDbContext;
		public IBaseRepository<User> Users {  get; private set; }
		public UnitOfWork(ApplicationDbContext applicationDbContext)
		{
			_applicationDbContext = applicationDbContext;
			Users = new BaseRepository<User>(_applicationDbContext);
		}

		public ValueTask DisposeAsync()
		{
			return _applicationDbContext.DisposeAsync();
		}

		public Task<int> SaveChangesAsync()
		{
			return _applicationDbContext.SaveChangesAsync();
		}

	}
}
