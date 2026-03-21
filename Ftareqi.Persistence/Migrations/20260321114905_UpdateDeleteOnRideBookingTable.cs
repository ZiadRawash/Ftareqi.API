using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ftareqi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeleteOnRideBookingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RideBookings_AspNetUsers_UserId",
                table: "RideBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_RideBookings_Rides_RideId",
                table: "RideBookings");

            migrationBuilder.AddForeignKey(
                name: "FK_RideBookings_AspNetUsers_UserId",
                table: "RideBookings",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RideBookings_Rides_RideId",
                table: "RideBookings",
                column: "RideId",
                principalTable: "Rides",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RideBookings_AspNetUsers_UserId",
                table: "RideBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_RideBookings_Rides_RideId",
                table: "RideBookings");

            migrationBuilder.AddForeignKey(
                name: "FK_RideBookings_AspNetUsers_UserId",
                table: "RideBookings",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RideBookings_Rides_RideId",
                table: "RideBookings",
                column: "RideId",
                principalTable: "Rides",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
