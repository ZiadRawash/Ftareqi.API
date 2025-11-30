using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Enums
{
	public enum DriverStatus
	{
		PendingImageUpload = 0,
		ImageUploadFailed= 1,
		Pending = 2,             
		Active = 3,                 
		Rejected = 4,            
		Suspended = 5,
		Expired=6
	}
}
