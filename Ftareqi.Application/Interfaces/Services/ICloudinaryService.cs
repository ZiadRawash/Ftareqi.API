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
		Task<Result<SavedImageDto>> UploadPhotoAsync(CloudinaryReqDto image);
		Task<Result<List<SavedImageDto>>> UploadPhotosAsync(List<CloudinaryReqDto> images);

		Task<Result> DeleteImage(string deleteId);
		Task<Result> DeleteImagesAsync(List<string> deleteIds);
	}
}
