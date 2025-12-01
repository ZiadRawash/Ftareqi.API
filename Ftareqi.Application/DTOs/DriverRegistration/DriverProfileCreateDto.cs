using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.DriverRegistration
{
	public class DriverProfileCreateDto
	{
		public string UserId { get; set; } = default!;
		public IFormFile? DriverProfilePhoto { get; set; }
		public IFormFile? DriverLicenseFront { get; set; }
		public IFormFile? DriverLicenseBack { get; set; }
		public DateTime LicenseExpiryDate { get; set; }
	}
}
