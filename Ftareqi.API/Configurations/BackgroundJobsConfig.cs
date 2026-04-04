using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Orchestrators;
using Ftareqi.Infrastructure.BackgroundJobs;
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
			recurringJobManager.AddOrUpdate<DriverJobs>(
				recurringJobId: "deactivate-expired-drivers",
				methodCall: job => job.DeactivateExpiredDriversAsync(),
				cronExpression: "40 19 * * *"
			);
			recurringJobManager.AddOrUpdate<IRideOrchestrator>(
				recurringJobId: "expire-pending-bookings",
				methodCall: job => job.HandleExpiredBookings(),
				cronExpression: "*/2 * * * *"
			);
			return Task.CompletedTask;
		}
	}
}
