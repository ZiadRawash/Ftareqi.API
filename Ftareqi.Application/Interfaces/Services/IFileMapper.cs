using Ftareqi.Application.DTOs.Cloudinary;
using Ftareqi.Domain.Enums;
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
		List<CloudinaryReqDto> MapFilesWithTypes(List<IFormFile> files, List<ImageType> imageTypes);
		CloudinaryReqDto MapFile(IFormFile file, ImageType imageType);	
	}
}