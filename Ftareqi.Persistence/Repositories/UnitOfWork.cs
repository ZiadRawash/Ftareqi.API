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
		public IBaseRepository<RefreshToken> RefreshTokens {  get; private set; }
		public IBaseRepository<OTP> OTPs {  get; private set; }
		public IBaseRepository<DriverProfile> DriverProfiles {  get; private set; }


		public UnitOfWork(ApplicationDbContext applicationDbContext)
		{
			_applicationDbContext = applicationDbContext;
			Users = new BaseRepository<User>(_applicationDbContext);
			RefreshTokens = new BaseRepository<RefreshToken>(_applicationDbContext);
			OTPs = new BaseRepository<OTP>(_applicationDbContext);
			DriverProfiles= new BaseRepository<DriverProfile>(_applicationDbContext);
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
