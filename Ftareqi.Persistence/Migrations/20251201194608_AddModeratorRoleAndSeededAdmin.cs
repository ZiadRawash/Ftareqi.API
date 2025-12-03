using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ftareqi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModeratorRoleAndSeededAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "b2c3d4e5-f678-90ab-cdef-1234567890ab");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "role-admin", "11111111-aaaa-bbbb-cccc-111111111111", "Admin", "ADMIN" },
                    { "role-moderator", "33333333-aaaa-bbbb-cccc-333333333333", "Moderator", "MODERATOR" },
                    { "role-user", "22222222-aaaa-bbbb-cccc-222222222222", "User", "USER" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAt", "DateOfBirth", "Email", "EmailConfirmed", "FullName", "Gender", "IsDeleted", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PenaltyCount", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UpdatedAt", "UserName" },
                values: new object[] { "admin1", 0, "55555555-aaaa-bbbb-cccc-555555555555", new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2004, 8, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@ftareqi.com", true, "Ziad Rawash", 1, false, false, null, "ADMIN@FTAREQI.COM", "ADMIN@FTAREQI.COM", "AQAAAAIAAYagAAAAELdvbbsNSTpjlcUQ5MZpRUQ5N2Bg93tunei18Crmhcqe3/dZJz5UIr9TK/4BXLuyUg==", 0, "+200000000000", false, "44444444-aaaa-bbbb-cccc-444444444444", false, null, "admin@ftareqi.com" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "role-admin", "admin1" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-moderator");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-user");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "role-admin", "admin1" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-admin");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin1");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "a1b2c3d4-e5f6-7890-abcd-ef1234567890", null, "Admin", "ADMIN" },
                    { "b2c3d4e5-f678-90ab-cdef-1234567890ab", null, "User", "USER" }
                });
        }
    }
}
