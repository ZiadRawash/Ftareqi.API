using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Enums
{
	public enum NotificationEventCode
	{
		bookingRequest = 101,
		bookingAccepted= 102,
		bookingDeclined = 103,
		bookingCanceled = 104,
		DriverCheckedIn =105,
		RideStarted = 106,
		RideCancelled=107,


		//Wallet
		WalletCharged = 200,
		WalletWithdrawn = 201,
		AmountReserved = 203,
		AmountReleased = 204,
		AmountTransferred= 205,

		//DriverRegistration
		Approved = 301,
		Rejected = 302,
		Expired=303,

		//Review
		ReviewAdded=500,

        //Report 
        ReportResolved=600,
        ReportRejected=601,

		//Ban
		BanActivated=700
    }
}
