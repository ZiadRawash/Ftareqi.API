using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	/// <summary>
	/// Generic abstraction for background job scheduling
	/// Application layer doesn't know if it's Hangfire, Quartz, or Azure Functions
	/// </summary>
	public interface IBackgroundJobService
	{
		/// <summary>
		/// Enqueue a job to run immediately in the background
		/// </summary>
		Task<string> EnqueueAsync<T>(Expression<Func<T, Task>> methodCall);

		/// <summary>
		/// Schedule a job to run after a delay
		/// </summary>
		Task<string> ScheduleAsync<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

		/// <summary>
		/// Schedule a recurring job
		/// </summary>
		Task<bool> DeleteJobAsync(string jobId);
	}
}