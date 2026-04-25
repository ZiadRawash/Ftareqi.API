using NetTopologySuite.Geometries;
using System;

namespace Ftareqi.Application.Common.Helpers
{
	public static class LocationHelper
	{
		/// <summary>
		/// Checks if the driver's current coordinates are within the allowed radius of a stored Point.
		/// </summary>
		public static bool IsWithinRadius(
			Point targetLocation,
			double currentLat,
			double currentLon,
			double radiusMeters)
		{
			if (targetLocation == null) return false;

			double startLat = targetLocation.Y;
			double startLon = targetLocation.X;

			const double EarthRadiusKm = 6371.0;

			var dLat = ToRadians(currentLat - startLat);
			var dLon = ToRadians(currentLon - startLon);

			var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
					Math.Cos(ToRadians(startLat)) * Math.Cos(ToRadians(currentLat)) *
					Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

			var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
			var distanceInMeters = EarthRadiusKm * c * 1000;

			return distanceInMeters <= radiusMeters;
		}

		private static double ToRadians(double angle)
		{
			return (Math.PI / 180) * angle;
		}
	}
}