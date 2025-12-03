using Ftareqi.Application.Common.Consts;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Persistence.Repositories
{
	public class DriverProfileRepository: BaseRepository<DriverProfile>, IDriverProfileRepository
	{
		private readonly ApplicationDbContext _context;
		private readonly DbSet<DriverProfile> _dbSet;
		public DriverProfileRepository(ApplicationDbContext context ) :base(context)
		{
		_context = context;	
			_dbSet= _context.Set<DriverProfile>();
		}

		public async Task<DriverProfile> GetDriverProfilesWithCarAsync(
			Expression<Func<DriverProfile, bool>> predicates)
		{
			IQueryable<DriverProfile> query = _dbSet
				.Where(predicates)
				.Include(x => x.User)
				.Include(d => d.Images)
				.Include(d => d.Car)
					.ThenInclude(c => c!.Images);
			var item = await query.FirstOrDefaultAsync();
			return item!;
		}
	}
}
