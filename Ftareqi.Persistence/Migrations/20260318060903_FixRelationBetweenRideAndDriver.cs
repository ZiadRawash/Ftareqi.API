using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ftareqi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationBetweenRideAndDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rides_DriverProfileId",
                table: "Rides");

            migrationBuilder.CreateIndex(
                name: "IX_Rides_DriverProfileId",
                table: "Rides",
                column: "DriverProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rides_DriverProfileId",
                table: "Rides");

            migrationBuilder.CreateIndex(
                name: "IX_Rides_DriverProfileId",
                table: "Rides",
                column: "DriverProfileId",
                unique: true);
        }
    }
}
