using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs;
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
		Task<Result<PaginatedResponse<DriverProfileWithUsernameDto>>> GetPendingDriverProfiles(PaginationReqDto page);
		Task<Result<DriverWithCarResponseDto>> GetDriverProfileDetails(int driverProfileId);
		Task<Result> ApproveDriverProfile(int profileId);
		Task<Result> RejectDriverProfile(int profileId);
	}
}
