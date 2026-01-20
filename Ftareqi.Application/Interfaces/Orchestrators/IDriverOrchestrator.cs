using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.DriverRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Orchestrators
{
	public interface IDriverOrchestrator
	{
		Task<Result<DriverProfileResponseDto>> CreateDriverProfileAsync(DriverProfileCreateDto driverDto);
		Task<Result<CarResponseDto>> CreateCarForDriverProfile(CarCreateDto carDto);
		Task<Result<PaginatedResponse<DriverProfileWithUsernameDto>>> GetPendingDriverProfiles(GenericQueryReq page);
		Task<Result<DriverWithCarResponseDto>> GetDriverProfileDetails(string userId);
		Task<Result> ApproveDriverProfile(int profileId);
		Task<Result> RejectDriverProfile(int profileId);
		Task<Result<DriverProfileResponseDto>> UpdateDriverProfileAsync(DriverProfileUpdateDto driverDto);
		Task<Result<CarResponseDto>> UpdateCarAsync(CarUpdateDto carDto);
	}
}
