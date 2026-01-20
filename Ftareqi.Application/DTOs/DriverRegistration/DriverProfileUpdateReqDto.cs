using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.DriverRegistration
{
	public class DriverProfileUpdateReqDto
	{
		public DateTime? LicenseExpiryDate { get; set; }
		public IFormFile? DriverProfilePhoto { get; set; }
		public IFormFile? DriverLicenseFront { get; set; }
		public IFormFile? DriverLicenseBack { get; set; }
	}
}
