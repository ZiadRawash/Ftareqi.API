using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class UserWallet
	{
		public int Id { get; set; }
		public decimal balance { get; set; }
		public decimal PendingBalance { get; set; }
		public bool IsLocked {  get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }

		public User User { get; set; }
		[ForeignKey(nameof(User))]
		public string UserId {  get; set; }

	}
}
