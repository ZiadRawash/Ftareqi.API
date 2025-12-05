using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Services;
using Hangfire;
using System.Threading.Tasks;

namespace Ftareqi.API.Configurations
{
	public static class BackgroundJobsConfig
	{
		public static Task RegisterJobs(IApplicationBuilder app)
		{
			var recurringJobManager = app.ApplicationServices
				.GetRequiredService<IRecurringJobManager>();
			recurringJobManager.AddOrUpdate<IDriverStatusJob>(
				recurringJobId: "deactivate-expired-drivers",
				methodCall: job => job.DeactivateExpiredDriversAsync(),
				cronExpression: "55 15 * * *"
			);
			return Task.CompletedTask;
		}
	}
}
