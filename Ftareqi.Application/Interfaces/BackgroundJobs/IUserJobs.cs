using Ftareqi.Application.DTOs.Cloudinary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.BackgroundJobs
{
	public interface IUserJobs
	{
		Task UploadProfileImage(CloudinaryReqDto image, string userId);
		Task DeleteProfileImage(string publicId);
	}
}
