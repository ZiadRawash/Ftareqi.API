using Ftareqi.Application.Interfaces.Services;

namespace Ftareqi.Application.Common.Helpers
{
	public static class DriverProfileCacheInvalidationExtensions
	{
		public static Task RemoveDriverProfileCachesAsync(this IDistributedCachingService cache, string userId)
		{
			return Task.WhenAll(
				cache.RemoveAsync(CacheKeys.UserProfile(userId)),
				cache.RemoveAsync(CacheKeys.DriverProfile(userId)),
				cache.RemoveAsync(CacheKeys.DriverCarProfile(userId)),
				cache.RemoveAsync(CacheKeys.PendingDriverProfilesFirstPage()));
		}
	}
}