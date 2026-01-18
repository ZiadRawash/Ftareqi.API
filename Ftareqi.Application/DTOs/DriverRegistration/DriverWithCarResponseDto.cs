using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.DriverRegistration
{
	public class DriverWithCarResponseDto
	{
		//driver
		public int ProfileId { get; set; }
		public string? FullName { get; set; }
		public string? PhoneNumber { get; set; }
		public DriverStatus DriverStatus { get; set; }
		public DateTime DriverLicenseExpiryDate { get; set; }
		public DateTime ProfileCreationDate { get; set; }
		public string? DriverPhoto {  get; set; }
		public string? DriverLicenseFront {  get; set; }
		public string? DriverLicenseBack {  get; set; }
		//car 
		public string Model { get; set; } = default!;
		public string? Color { get; set; } = default!;
		public string? Plate { get; set; } = default!;
		public int NumOfSeats { get; set; }
		public string? CarPhoto { get; set; }
		public string? CarLicenseFront { get; set; }
		public string? CarLicenseBack { get; set; }
		public DateTime? CarLicenseExpiryDate { get; set; }


	}
}
