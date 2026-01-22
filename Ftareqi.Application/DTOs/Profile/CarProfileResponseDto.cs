using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Profile
{
	public class CarProfileResponseDto
	{
		public int Id { get; set; }
		public string Model { get; set; } = default!;
		public string Color { get; set; } = default!;
		public string Plate { get; set; } = default!;
		public int NumOfSeats { get; set; }
		public DateTime LicenseExpiryDate { get; set; }
		public DateTime CreatedAt { get; set; }

		public string? CarPhoto { get; set; }
		public string? CarLicenseFront { get; set; }
		public string? CarLicenseBack { get; set; }
	}
}
