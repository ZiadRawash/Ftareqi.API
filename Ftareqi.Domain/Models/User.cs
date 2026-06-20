using Ftareqi.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace Ftareqi.Domain.Models
{
	public class User : IdentityUser
	{
		public required string FullName { get; set; }

		public Gender Gender { get; set; }
		 

		public required DateTime DateOfBirth { get; set; }

		public DateTime CreatedAt { get; set; }= DateTime.UtcNow;

		public DateTime? UpdatedAt { get; set; }

		public bool IsDeleted { get; set; }

		public int PenaltyCount { get; set; }
		public Image? Image { get; set; }
		public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
		public DriverProfile? DriverProfile { get; set; }
		public UserWallet UserWallet { get; set; }
		public List<Notification> Notifications { get; set; }= new List<Notification>();
		public ICollection<FcmToken> FcmTokens { get; set; } = new List<FcmToken>();
		public ICollection<RideBooking> RideBookings { get; set; } = new List<RideBooking>();
		public ICollection<Report> ReportsMade { get; set; } = new List<Report>();
		public ICollection<Report> ReportsReceived { get; set; } = new List<Report>();
		public ICollection<Ban> BansIssued { get; set; } = new List<Ban>();
	}
}
