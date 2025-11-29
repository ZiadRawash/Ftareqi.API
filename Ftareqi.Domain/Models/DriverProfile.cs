using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Domain.Models
{
	public class DriverProfile
	{
		public int Id { get; set; }
		public DateTime LicenseExpiryDate { get; set; }	
		public bool IsVerified { get; set; }
		public DateTime CreatedAt {  get; set; }=DateTime.UtcNow;
		public DateTime UpdatedAt {  get; set; }
		public bool IsDeleted {  get; set; }

		public ICollection<Image> Images { get; set; } = new List<Image>();
		public User? User { get; set; }
		[ForeignKey(nameof(User))]
		public required string UserId { get; set; }
	}
}
