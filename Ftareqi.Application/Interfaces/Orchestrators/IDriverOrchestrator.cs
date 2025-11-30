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
		Task<Result<DriverProfileResponseDto>> CreateDriverProfileAsync(DriverProfileReqDto driverDto);
	}
}
