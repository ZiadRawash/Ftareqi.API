using Ftareqi.API;
using StackExchange.Redis;
using System.Net;
using System.Security.Claims;

public class TokenBucketMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IConnectionMultiplexer _redis;
	private readonly AuthTokenBucketOptions _authOptions;
	private readonly UnauthTokenBucketOptions _unauthOptions;

	public TokenBucketMiddleware(
		RequestDelegate next,
		IConnectionMultiplexer redis,
		AuthTokenBucketOptions authOptions,
		UnauthTokenBucketOptions unauthOptions)
	{
		_next = next;
		_redis = redis;
		_authOptions = authOptions;
		_unauthOptions = unauthOptions;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		var clientId = GetClientIdentifier(context);

		if (clientId == null)
		{
			await _next(context);
			return;
		}

		TokenBucketOptions options = clientId.StartsWith("ip:") ? _unauthOptions : _authOptions;
		var allowed = await TryConsumeTokenAsync(clientId, options);

		if (!allowed)
		{
			context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
			context.Response.Headers["Retry-After"] = ((int)Math.Ceiling(1.0 / options.RefillRatePerSecond)).ToString();
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{\"message\": \"Rate limit exceeded. Try again later.\"}");
			return;
		}

		await _next(context);
	}

	private async Task<bool> TryConsumeTokenAsync(string clientId, TokenBucketOptions options)
	{
		try
		{
			var db = _redis.GetDatabase();
			var key = $"token_bucket:{clientId}";
			var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

			var script = @"
            local key        = KEYS[1]
            local now        = tonumber(ARGV[1])
            local capacity   = tonumber(ARGV[2])
            local refillRate = tonumber(ARGV[3])
            local requested  = tonumber(ARGV[4])

            local bucket     = redis.call('HMGET', key, 'tokens', 'last_refill')
            local tokens     = tonumber(bucket[1]) or capacity
            local lastRefill = tonumber(bucket[2]) or now

            local elapsed   = math.max(0, now - lastRefill)
            local newTokens = elapsed * refillRate
            tokens = math.min(capacity, tokens + newTokens)

            if tokens >= requested then
                tokens = tokens - requested
                redis.call('HMSET', key, 'tokens', tokens, 'last_refill', now)
                redis.call('PEXPIRE', key, 60000)
                return 1
            else
                redis.call('HMSET', key, 'tokens', tokens, 'last_refill', now)
                redis.call('PEXPIRE', key, 60000)
                return 0
            end
        ";

			var result = await db.ScriptEvaluateAsync(script,
				keys: new RedisKey[] { key },
				values: new RedisValue[]
				{
				now,
				options.Capacity,
				options.RefillRatePerSecond / 1000.0,
				1
				});

			return (int)result == 1;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[RateLimit ERROR] {ex.GetType().Name}: {ex.Message}");
			return true; 
		}
	}

	private string? GetClientIdentifier(HttpContext context)
	{
		//authenticated user
		var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!string.IsNullOrEmpty(userId))
			return $"user:{userId}";

		var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
			  ?? context.Connection.RemoteIpAddress?.ToString();

		if (!string.IsNullOrEmpty(ip))
			return $"ip:{ip}";

		return null;
	}
}