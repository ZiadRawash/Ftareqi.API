using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Constants
{
	public class RidePolicies
	{
		/// <summary>
		/// The maximum distance (in meters) between the driver and the start location 
		/// to consider the driver "Arrived".
		/// </summary>
		public const double ArrivalRadiusMeters = 200.0;


		/// <summary>
		/// The buffer time (in minutes) allowed for the driver to arrive after without strikes
		/// the scheduled waiting start time.
		/// </summary>
		public const int ArrivalGracePeriodMinutes = 7;

		/// <summary>
		/// The hard deadline (in minutes) after the scheduled departure time. 
		/// If the ride hasn't started by this time, it is automatically cancelled.
		/// </summary>
		public const int AutoCancellationDeadlineMinutes = 10;


		/// <summary>
		/// The maximum number of critical late arrivals allowed before a driver 
		/// faces a temporary or permanent ban.
		/// </summary>
		public const int MaxLateArrivalStrikes = 3;
	}
}
