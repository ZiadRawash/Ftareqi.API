using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.LiveTracking;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Clients
{
	public interface IliveTrackingClient
	{
		Task ReceiveDriverCoordinates(DriveLocationResultDto model);
	}
}
