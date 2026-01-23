using Ftareqi.Application.Interfaces.Services;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class HangfireBackgroundJobService : IBackgroundJobService
	{
		private readonly IBackgroundJobClient _hangfireClient;
		private readonly ILogger<HangfireBackgroundJobService> _logger;

		public HangfireBackgroundJobService(
			IBackgroundJobClient hangfireClient,
			ILogger<HangfireBackgroundJobService> logger)
		{
			_hangfireClient = hangfireClient;
			_logger = logger;
		}

		public Task<string> EnqueueAsync<T>(Expression<Func<T, Task>> methodCall)
		{
			var jobId = _hangfireClient.Enqueue(methodCall);
			_logger.LogInformation("Hangfire job {jobId} enqueued for {type}", jobId, typeof(T).Name);
			return Task.FromResult(jobId);
		}

		public Task<string> ScheduleAsync<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
		{
			var jobId = _hangfireClient.Schedule(methodCall, delay);
			_logger.LogInformation("Hangfire job {jobId} scheduled for {type} after {delay}",
				jobId, typeof(T).Name, delay);
			return Task.FromResult(jobId);
		}
		public Task<bool> DeleteJobAsync(string jobId)
		{
			var result = _hangfireClient.Delete(jobId);
			_logger.LogInformation("Job {jobId} deletion result: {result}", jobId, result);
			return Task.FromResult(result);
		}
	}
}