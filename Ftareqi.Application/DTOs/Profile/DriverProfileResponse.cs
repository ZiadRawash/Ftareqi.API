using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Profile
{
	public class DriverProfileResponse
	{
		public int Id { get; set; }
		public DateTime LicenseExpiryDate { get; set; }
		public DriverStatus Status { get; set; }
		public DateTime CreatedAt { get; set; } 
		public string? DriverProfilePhoto {  get; set; }
		public string? DriverLicenseFront {  get; set; }
		public string? DriverLicenseBack {  get; set; }
	}
}
