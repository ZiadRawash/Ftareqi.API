using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class RidePreferences
	{
		public int Id { get; set; }
		public bool MusicAllowed { get; set; }
		public bool NoSmoking { get; set; }
		public bool PetsWelcomed { get; set; }
		public bool OpenToConversation { get; set; }
		public Ride Ride { get; set; } = null!;
		public int RideId { get; set; }
	}
}
