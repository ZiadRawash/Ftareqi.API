using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ftareqi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkRideBookingsWithTransiactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RideBookingId",
                table: "WalletTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "RideBookings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_RideBookingId",
                table: "WalletTransactions",
                column: "RideBookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_WalletTransactions_RideBookings_RideBookingId",
                table: "WalletTransactions",
                column: "RideBookingId",
                principalTable: "RideBookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WalletTransactions_RideBookings_RideBookingId",
                table: "WalletTransactions");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_RideBookingId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "RideBookingId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "RideBookings");
        }
    }
}
