using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Cloudinary;
using Ftareqi.Application.DTOs.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface ICloudinaryService
	{
		Task<Result<SavedImageDto>> UploadPhotos(CloudinaryReqDto Image);
		Task<Result> DeleteImage(string deleteId);
	}
}
