using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ftareqi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFcmTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FcmToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FcmTokenUpdatedAt",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "FcmToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FcmToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FcmToken_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FcmToken_UserId",
                table: "FcmToken",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FcmToken");

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
    }
}
