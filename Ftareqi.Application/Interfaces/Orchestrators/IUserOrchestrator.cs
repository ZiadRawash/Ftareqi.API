using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Profile;
using Ftareqi.Application.DTOs.User;
using Ftareqi.Application.QueryEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Orchestrators
{
	public interface IUserOrchestrator
	{
		Task<Result<PaginatedResponse<UserDriveStatusDto>>> GetUserWithDriverStatus(UserQueryDto reqModel);
		Task<Result<UserWithRolesDto>> GetUserDetails(string userId);
		Task<Result<ProfileResponseDto>> GetProfile(string userId);
		Task<Result> UploadProfileImage(string userId, ProfileImageReqDto image);
		Task<Result> UpdateProfileImage(string userId, ProfileImageReqDto imageDto);
	}
}
