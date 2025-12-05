using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.DriverRegistration
{
	public class CarCreateDto
	{
		public string? UserId { get; set; }
		public string? Model { get; set; }
		public string? Color { get; set; } = default!;
		public string? Palette { get; set; } = default!;
		public int NumOfSeats { get; set; }
		public IFormFile? CarPhoto { get; set; }
		public IFormFile? CarLicenseFront { get; set; }
		public IFormFile? CarLicenseBack { get; set; }
		public DateTime LicenseExpiryDate {  get; set; }
	}
}
