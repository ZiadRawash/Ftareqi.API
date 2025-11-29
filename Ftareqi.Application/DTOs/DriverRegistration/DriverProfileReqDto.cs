using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.DriverRegistration
{
	public class DriverProfileReqDto
	{
		public string  PhoneNumber { get; set; }=string.Empty;
		public IFormFile? DriverPhoto{ get; set; } 
		public IFormFile? DriverLicensePhoto { get; set; } 
		public DateTime LicenseExpiryDate { get; set; }
	}
}
