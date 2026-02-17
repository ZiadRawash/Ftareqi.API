using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ftareqi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWalletAndPaymentTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingBalance",
                table: "UserWallet");

            migrationBuilder.RenameColumn(
                name: "balance",
                table: "UserWallet",
                newName: "Balance");

            migrationBuilder.RenameColumn(
                name: "method",
                table: "PaymentTransactions",
                newName: "Method");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "WalletTransactions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserWallet",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<decimal>(
                name: "LockedBalance",
                table: "UserWallet",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PaymentTransactions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedBalance",
                table: "UserWallet");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PaymentTransactions");

            migrationBuilder.RenameColumn(
                name: "Balance",
                table: "UserWallet",
                newName: "balance");

            migrationBuilder.RenameColumn(
                name: "Method",
                table: "PaymentTransactions",
                newName: "method");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "WalletTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserWallet",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PendingBalance",
                table: "UserWallet",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
