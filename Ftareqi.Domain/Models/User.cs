using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace Ftareqi.Domain.Models
{
	public class User : IdentityUser
	{
		public required string FullName { get; set; }

		public string? ProfilePictureUrl { get; set; }

		public DateTime? DateOfBirth { get; set; }

		public DateTime CreatedAt { get; set; }= DateTime.UtcNow;

		public DateTime? UpdatedAt { get; set; }

		public bool IsDeleted { get; set; }

		public int PenaltyCount { get; set; }
	}
}
