using Ftareqi.Application.DTOs.Cloudinary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.BackgroundJobs
{
	public interface ICarJobs
	{
		Task DeleteCarImagesAsync(List<string> publicIds);
		Task UploadCarImagesAsync(int carId, List<CloudinaryReqDto> images);
	}
}
