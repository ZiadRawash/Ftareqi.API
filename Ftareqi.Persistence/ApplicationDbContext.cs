using System;
using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ftareqi.Persistence
{
	public class ApplicationDbContext : IdentityDbContext<User>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public DbSet<RefreshToken> RefreshTokens { get; set; }
		public DbSet<OTP> OTPs { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<IdentityRole>().HasData(
				new IdentityRole
				{
					Id = "role-admin",
					Name = "Admin",
					NormalizedName = "ADMIN",
					ConcurrencyStamp = "11111111-aaaa-bbbb-cccc-111111111111"
				},
				new IdentityRole
				{
					Id = "role-user",
					Name = "User",
					NormalizedName = "USER",
					ConcurrencyStamp = "22222222-aaaa-bbbb-cccc-222222222222"
				},
				new IdentityRole
				{
					Id = "role-moderator",
					Name = "Moderator",
					NormalizedName = "MODERATOR",
					ConcurrencyStamp = "33333333-aaaa-bbbb-cccc-333333333333"
				}
			);
			//password is Admin@123
			builder.Entity<User>().HasData(
				new User
				{
					Id = "admin1",
					UserName = "admin@ftareqi.com",
					NormalizedUserName = "ADMIN@FTAREQI.COM",
					Email = "admin@ftareqi.com",
					NormalizedEmail = "ADMIN@FTAREQI.COM",
					EmailConfirmed = true,
					SecurityStamp = "44444444-aaaa-bbbb-cccc-444444444444",

					FullName = "Ziad Rawash",
					Gender = Ftareqi.Domain.Enums.Gender.Male,
					DateOfBirth = new DateTime(2004, 8, 11),
					CreatedAt = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
					PenaltyCount = 0,
					IsDeleted = false,

					PhoneNumber = "+200000000000",
					PhoneNumberConfirmed = true,
					TwoFactorEnabled = false,

					PasswordHash = "AQAAAAIAAYagAAAAELdvbbsNSTpjlcUQ5MZpRUQ5N2Bg93tunei18Crmhcqe3/dZJz5UIr9TK/4BXLuyUg==",
					ConcurrencyStamp = "55555555-aaaa-bbbb-cccc-555555555555"
				}
			);

			builder.Entity<IdentityUserRole<string>>().HasData(
				new IdentityUserRole<string>
				{
					UserId = "admin1",
					RoleId = "role-admin"
				}
			);
		}
	}
}
