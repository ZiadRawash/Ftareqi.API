using Ftareqi.Application.DTOs.BackgroundJobs;
using Ftareqi.Application.DTOs.Cloudinary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.BackgroundJobs
{
	public interface IDriverImageUploadJob
	{
		Task UploadDriverImagesAsync(
			int driverProfileId,
			string userId,
			List<CloudinaryReqDto> imagesToUpload);
	}
}
