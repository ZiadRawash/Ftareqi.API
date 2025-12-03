using Ftareqi.Application.Common.Consts;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Repositories
{
	public interface IDriverProfileRepository:IBaseRepository<DriverProfile>
	{
	Task<DriverProfile> GetDriverProfilesWithCarAsync(
			Expression<Func<DriverProfile, bool>> predicates);
	}
}
