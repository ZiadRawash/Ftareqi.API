using Ftareqi.Application.Common;
using Ftareqi.Application.Interfaces.Services;

namespace Ftareqi.Application.Common.Helpers
{
	public static class WalletCacheInvalidationExtensions
	{
		public static Task RemoveWalletCachesAsync(this IDistributedCachingService cache, string userId)
		{
			return Task.WhenAll(
				cache.RemoveAsync(CacheKeys.Wallet(userId)),
				cache.RemoveAsync(CacheKeys.WalletTransactionsFirstPage(userId)));
		}

		public static bool IsWalletTransactionsFirstPage(this GenericQueryReq query)
		{
			return query.Page == 1 && query.PageSize == 10;
		}
	}
}