using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.Cloudinary
{
	public class CloudinaryReqDto
	{
		public required string FileName { get; set; }
		public required Stream FileStream { get; set; }
		public required ImageType imageType { get; set; }
	}
}
