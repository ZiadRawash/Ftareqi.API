using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Profile
{
	public class ProfileImageReqDto
	{
		public IFormFile? Image {  get; set; }
	}
}
