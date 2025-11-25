using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

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
			List<IdentityRole> roles = new List<IdentityRole>
			{
				new IdentityRole
				{
					Id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
					Name = "Admin",
					NormalizedName = "ADMIN"
				},
				new IdentityRole
				{
					Id = "b2c3d4e5-f678-90ab-cdef-1234567890ab",
					Name = "User",
					NormalizedName = "USER"
				},
			};

			builder.Entity<IdentityRole>().HasData(roles);

			base.OnModelCreating(builder);
		}
	}
}
