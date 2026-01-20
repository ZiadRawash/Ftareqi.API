using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.DriverRegistration
{
	public class CarUpdateReqDto
	{
		public string? Color { get; set; }
		public string? Model { get; set; }
		public string? Plate { get; set; }
		public int? NumOfSeats { get; set; }
		public DateTime? LicenseExpiryDate { get; set; }
		public IFormFile? CarPhoto { get; set; }
		public IFormFile? CarLicenseBack { get; set; }
		public IFormFile? CarLicenseFront { get; set; }
	}


}
