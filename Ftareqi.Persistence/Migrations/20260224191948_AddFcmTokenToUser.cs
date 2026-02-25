using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ftareqi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFcmTokenToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FcmToken",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FcmTokenUpdatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin1",
                columns: new[] { "FcmToken", "FcmTokenUpdatedAt" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FcmToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FcmTokenUpdatedAt",
                table: "AspNetUsers");
        }
    }
}
