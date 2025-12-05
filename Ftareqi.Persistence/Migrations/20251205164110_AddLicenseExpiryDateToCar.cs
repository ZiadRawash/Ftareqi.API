using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ftareqi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseExpiryDateToCar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Car_DriverProfileId",
                table: "Car");

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseExpiryDate",
                table: "Car",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Car_DriverProfileId",
                table: "Car",
                column: "DriverProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Car_DriverProfileId",
                table: "Car");

            migrationBuilder.DropColumn(
                name: "LicenseExpiryDate",
                table: "Car");

            migrationBuilder.CreateIndex(
                name: "IX_Car_DriverProfileId",
                table: "Car",
                column: "DriverProfileId");
        }
    }
}
