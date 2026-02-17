using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs
{
	public class WalletResDto
	{
		public int Id { get; set; }
		public decimal Balance{ get; set; }
		public decimal LockedBalance { get; set; }
		public bool IsLocked { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}
}
