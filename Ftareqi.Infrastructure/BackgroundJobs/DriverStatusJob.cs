using Ftareqi.Application.Common.Consts;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ftareqi.Infrastructure.BackgroundJobs
{
	public class DriverStatusJob : IDriverStatusJob
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserClaimsService _userClaimsService;
		public DriverStatusJob(IUnitOfWork unitOfWork,IUserClaimsService claimsService)
		{
			_unitOfWork = unitOfWork;
			_userClaimsService = claimsService;		
		}

		public async Task DeactivateExpiredDriversAsync()
		{

			var dateRN = DateTime.UtcNow.AddDays(1);
			var varProfilesFound = await _unitOfWork.DriverProfiles.FindAllAsTrackingAsync(
				dp => dp.LicenseExpiryDate <= dateRN
				   || (dp.Car != null && dp.Car.LicenseExpiryDate <= dateRN),
				x => x.Car!
			);
			foreach (var profile  in varProfilesFound)
			{
				profile.Status = DriverStatus.Expired;
				await _userClaimsService.RemoveClaimAsync(profile.UserId, CustomClaimTypes.IsDriver);
			}
			_unitOfWork.DriverProfiles.UpdateRange(varProfilesFound);
			await _unitOfWork.SaveChangesAsync();
		}
	}
}
