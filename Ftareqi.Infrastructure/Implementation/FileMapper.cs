using Ftareqi.Application.DTOs.Files;
using Ftareqi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class FileMapper : IFileMapper
	{
		public List<CloudinaryReqDto> MapFiles(List<IFormFile> files)
		{
			return files.Select(f => new CloudinaryReqDto
			{
				FileName = f.Name,
				FileStream = f.OpenReadStream()
			}).ToList();
		}
	}
}
