using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IDistributedCachingService
	{
		Task<T?> GetAsync<T>(string key);

		Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);

		Task RemoveAsync(string key);

		Task RefreshAsync(string key);
	}
}
