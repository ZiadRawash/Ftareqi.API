using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ftareqi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedDriverProfileWithDriverStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "DriverProfile");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "DriverProfile",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "DriverProfile");

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "DriverProfile",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
