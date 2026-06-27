using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Ban;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IBanService
	{
		Task<Result> BanDriverProfileAsync(string driverUserId, CreateBanDto model, string moderatorUserId);
		Task<Result<BanSummaryDto>> GetSummaryAsync();
		Task<Result<PaginatedResponse<BannedProfileDto>>> GetBannedProfilesAsync(GenericQueryReq request);
		Task<Result<DriverBanHistoryDto>> GetDriverBanHistoryAsync(string userId);
		Task<Result<bool>> IsUserBannedAsync(string userId);

	}
}