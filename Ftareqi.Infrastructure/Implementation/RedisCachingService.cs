using Ftareqi.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class RedisCachingService : IDistributedCachingService
	{
		private readonly IDistributedCache _cache;
		private readonly ILogger<RedisCachingService> _logger;

		private const int DefaultExpirationSeconds = 300;

		public RedisCachingService(IDistributedCache cache, ILogger<RedisCachingService> logger)
		{
			_cache = cache;
			_logger = logger;
		}

		public async Task<T?> GetAsync<T>(string key)
		{
			ArgumentException.ThrowIfNullOrEmpty(key);
			try
			{
				var item = await _cache.GetStringAsync(key);
				if (item == null)
				{
					_logger.LogDebug("Cache miss for key: {Key}", key);
					return default;
				}
				return JsonSerializer.Deserialize<T>(item, _jsonOptions);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to get cache key: {Key}", key);
				return default;
			}
		}

		public async Task RefreshAsync(string key)
		{
			ArgumentException.ThrowIfNullOrEmpty(key);
			try
			{
				await _cache.RefreshAsync(key);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to refresh cache key: {Key}", key);
			}
		}

		public async Task RemoveAsync(string key)
		{
			ArgumentException.ThrowIfNullOrEmpty(key);
			try
			{
				await _cache.RemoveAsync(key);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to remove cache key: {Key}", key);
			}
		}

		public async Task SetAsync<T>(
			string key,
			T value,
			TimeSpan? absoluteExpiration = null,
			TimeSpan? slidingExpiration = null)
		{
			ArgumentException.ThrowIfNullOrEmpty(key);
			try
			{
				var options = new DistributedCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = absoluteExpiration
						?? TimeSpan.FromSeconds(DefaultExpirationSeconds)
				};
				if (slidingExpiration.HasValue)
					options.SlidingExpiration = slidingExpiration;

				var serializedData = JsonSerializer.Serialize(value, _jsonOptions);
				await _cache.SetStringAsync(key, serializedData, options);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to set cache key: {Key}", key);
			}
		}

		private static readonly JsonSerializerOptions _jsonOptions = new()
		{
			PropertyNameCaseInsensitive = true
		};
	}
}
