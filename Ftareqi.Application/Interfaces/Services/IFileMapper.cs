using Ftareqi.Application.DTOs.Files;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IFileMapper
	{
		List<CloudinaryReqDto> MapFiles(List<IFormFile> files);
	}
}
