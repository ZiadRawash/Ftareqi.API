using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Profile
{
	public class PublicDriverProfileDto
	{
		public string? Name { get; set; }
		public Gender Gender { get; set; }
		public DateTime ? JoinedAt { get; set; }
		public string ? DriverImg {  get; set; }
		public Double? Rating { get; set; }
		public int TripsTaken { get; set; }
		public string? CarImg { get; set; }
		public string? CarModel { get; set; }
		public string? CarPlate { get; set; }
	}
}
