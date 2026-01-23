using Ftareqi.Application.DTOs.Cloudinary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.BackgroundJobs
{
	public interface IDriverJobs
	{
		Task DeleteDriverImagesAsync(List<string> publicIds);
		Task UploadDriverImagesAsync(int driverProfileId, string userId, List<CloudinaryReqDto> imagesToUpload);
		Task DeactivateExpiredDriversAsync();
	}

}
