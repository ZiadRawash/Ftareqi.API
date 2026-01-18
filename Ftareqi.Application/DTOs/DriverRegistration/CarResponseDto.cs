using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.DriverRegistration
{
	public class CarResponseDto
	{	
		public int CarId { get; set; }
		public int DriverProfileId { get; set; }
		public int NumOfSeats { get; set; }
		public string? Model { get; set; }
		public string? Plate { get; set; }
		public string? Color { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime LicenseExpiryDate { get; set; }
	}
}
