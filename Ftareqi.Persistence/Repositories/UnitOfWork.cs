using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
		public IBaseRepository<RefreshToken> RefreshTokens {  get; private set; }
		public IBaseRepository<OTP> OTPs {  get; private set; }
		public IDriverProfileRepository DriverProfiles {  get; private set; }
		public IBaseRepository<Image> Images {  get; private set; }
		public IBaseRepository<Car> Cars {  get; private set; }

		public UnitOfWork(ApplicationDbContext applicationDbContext)
		{
			_applicationDbContext = applicationDbContext;
			Users = new BaseRepository<User>(_applicationDbContext);
			RefreshTokens = new BaseRepository<RefreshToken>(_applicationDbContext);
			OTPs = new BaseRepository<OTP>(_applicationDbContext);
			DriverProfiles= new DriverProfileRepository(_applicationDbContext);
			Images= new BaseRepository<Image>(_applicationDbContext);
			Cars= new BaseRepository<Car>(_applicationDbContext);
		}
		public ValueTask DisposeAsync()
		{
			return _applicationDbContext.DisposeAsync();
		}

		public Task<int> SaveChangesAsync()
		{
			return _applicationDbContext.SaveChangesAsync();
		}

		public async Task<IDbContextTransaction> BeginTransactionAsync()
		{
			return await _applicationDbContext.Database.BeginTransactionAsync();
		}
	}
}
