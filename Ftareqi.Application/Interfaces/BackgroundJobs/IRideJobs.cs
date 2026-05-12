using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.BackgroundJobs
{
	public interface IRideJobs
	{
		/// <summary>
		/// Handles rides that were not started by the driver on time.
		/// Cancels the ride, releases locked funds, and notifies affected users.
		/// </summary>
		/// <param name="rideId">The ride id to process.</param>
		Task HandleNotStartedRidesAsync(int rideId);
	}
}
