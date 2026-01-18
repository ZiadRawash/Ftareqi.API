using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.DriverRegistration
{
	public class CarReqDto
	{
		public string? Model { get; set; } 
		public string? Color { get; set; } = default!;
		public string? Plate { get; set; } = default!;
		public DateTime LicenseExpiryDate { get; set; }
		public int NumOfSeats { get; set; }
		public IFormFile? CarPhoto { get; set; }
		public IFormFile? CarLicenseFront { get; set; }
		public IFormFile ? CarLicenseBack { get; set; }
	}
}
