using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.DTOs.Profile;
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
		Task<Result<DriverWithCarResponseDto>> GetDriverDetails(int driverId);
		Task<Result> ApproveDriverProfile(int profileId);
		Task<Result> RejectDriverProfile(int profileId);
		Task<Result<DriverProfileResponseDto>> UpdateDriverProfileAsync(DriverProfileUpdateDto driverDto);
		Task<Result<CarResponseDto>> UpdateCarAsync(CarUpdateDto carDto);
		Task<Result<DriverProfileResponse>> GetDriverProfile(string id);
		Task<Result<CarProfileResponseDto>> GetCarByDriverProfileId(int driverProfileId);

	}
}
