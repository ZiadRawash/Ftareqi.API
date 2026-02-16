using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Enums.PaymentEnums
{
	public enum PaymobOrderStatus
	{
		Pending,           // Order created, payment not yet attempted

		Processing,        // Payment is being processed

		Paid,             // Payment completed successfully
		Completed,        // Order fulfilled (sometimes used instead of Paid)

		Failed,           // Payment failed
		Declined,         // Payment declined by bank/provider
		Expired,          // Payment link/session expired
		Canceled,         // Order canceled
		Refunded,         // Payment was refunded

		PendingVerification,  // Awaiting verification
		OnHold            // Payment on hold for review
	}
}
