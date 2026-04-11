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
		//Wallet
		WalletCharged = 200,
		WalletWithdrawn = 201,
		AmountReserved = 203,
		AmountReleased = 204,

		//DriverRegistration
		Approved = 301,
		Rejected = 302,
		Expired=303


	}
}
