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
		public ICollection<Image> Images { get; set; } = new List<Image>();
		public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
		public DriverProfile? DriverProfile { get; set; }
	}
}
