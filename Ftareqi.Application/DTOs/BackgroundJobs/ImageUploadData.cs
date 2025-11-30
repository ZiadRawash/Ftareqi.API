using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.BackgroundJobs
{
	public class ImageUploadData
	{
		public required string FileName { get; set; }
		public required byte[] FileBytes { get; set; }
		public required ImageType ImageType { get; set; }
	}
}
