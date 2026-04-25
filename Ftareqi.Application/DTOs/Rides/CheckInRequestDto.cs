using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Rides
{
	public class CheckInRequestDto
	{

		[Required]
		public double Latitude { get; set; }

		[Required]
		public double Longitude { get; set; }
	}
}
