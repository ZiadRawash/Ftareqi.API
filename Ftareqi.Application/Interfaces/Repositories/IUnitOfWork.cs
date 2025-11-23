using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Repositories
{
	public interface IUnitOfWork: IAsyncDisposable	
	{
		IBaseRepository<User> Users {  get; }
		IBaseRepository <RefreshToken > RefreshTokens { get; }
		Task<int> SaveChangesAsync();
	}
}
