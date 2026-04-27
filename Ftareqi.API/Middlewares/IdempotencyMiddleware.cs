using Ftareqi.API.CustomedAttributes;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging; 
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ftareqi.API.Middlewares
{
	public class IdempotencyMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<IdempotencyMiddleware> _logger; 
		private const string HeaderName = "X-Idempotency-Key";

		public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
			var attribute = endpoint?.Metadata.GetMetadata<Idempotent>();

			if (attribute == null)
			{
				await _next(context);
				return;
			}

			if (context.Request.Method != HttpMethods.Post)
			{
				await _next(context);
				return;
			}

			if (!context.Request.Headers.TryGetValue(HeaderName, out var key))
			{
				_logger.LogWarning("Idempotency-Key header is missing for an idempotent endpoint: {Path}", context.Request.Path);
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				await context.Response.WriteAsync("Missing Idempotency-Key header");
				return;
			}

			var cacheService = context.RequestServices.GetRequiredService<IDistributedCachingService>();
			var cacheKey = key.ToString();
			var redisKey = CacheKeys.IdempotencyResponse(cacheKey);

			var cachedResponse = await cacheService.GetAsync<CachedResponse>(redisKey);

			if (cachedResponse != null)
			{
				_logger.LogInformation("Idempotency hit! Returning cached response for Key: {IdempotencyKey}", cacheKey);
				await WriteCachedResponse(context, cachedResponse);
				return;
			}

			_logger.LogInformation("Idempotency miss. Processing request for Key: {IdempotencyKey}", cacheKey);

			var originalBody = context.Response.Body;
			using var memoryStream = new MemoryStream();
			context.Response.Body = memoryStream;

			try
			{
				await _next(context);

				if (context.Response.StatusCode < 500)
				{
					memoryStream.Position = 0;
					var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

					await StoreResponseAsync(cacheService, cacheKey, context.Response.StatusCode, context.Response.ContentType, responseBody);
					_logger.LogInformation("Response cached successfully for Key: {IdempotencyKey}", cacheKey);
				}

				memoryStream.Position = 0;
				await memoryStream.CopyToAsync(originalBody);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred during idempotent request processing for Key: {IdempotencyKey}", cacheKey);
				throw; 
			}
			finally
			{
				context.Response.Body = originalBody;
			}
		}

		private async Task WriteCachedResponse(HttpContext context, CachedResponse cached)
		{
			context.Response.StatusCode = cached.StatusCode;

			if (!string.IsNullOrEmpty(cached.ContentType))
				context.Response.ContentType = cached.ContentType;

			if (!string.IsNullOrEmpty(cached.Body))
				await context.Response.WriteAsync(cached.Body);
		}

		private async Task StoreResponseAsync(
			IDistributedCachingService cacheService,
			string key,
			int statusCode,
			string? contentType,
			string body)
		{
			var model = new CachedResponse
			{
				StatusCode = statusCode,
				Body = body,
				ContentType = contentType,
			};

			await cacheService.SetAsync(
				CacheKeys.IdempotencyResponse(key),
				model,
				TimeSpan.FromHours(12));
		}
	}

	public class CachedResponse
	{
		public int StatusCode { get; set; }
		public string? ContentType { get; set; }
		public string? Body { get; set; }
	}
}