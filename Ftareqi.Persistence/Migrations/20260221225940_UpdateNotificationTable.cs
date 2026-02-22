using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ftareqi.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class UpdateNotificationTable : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// 1️⃣ Add a new temporary column with int IDENTITY
			migrationBuilder.AddColumn<int>(
				name: "NewId",
				table: "Notifications",
				type: "int",
				nullable: false,
				defaultValue: 0)
				.Annotation("SqlServer:Identity", "1, 1");

			// 2️⃣ Drop old primary key
			migrationBuilder.DropPrimaryKey(
				name: "PK_Notifications",
				table: "Notifications");

			// 3️⃣ Drop old Id column
			migrationBuilder.DropColumn(
				name: "Id",
				table: "Notifications");

			// 4️⃣ Rename NewId to Id
			migrationBuilder.RenameColumn(
				name: "NewId",
				table: "Notifications",
				newName: "Id");

			// 5️⃣ Add primary key on new Id
			migrationBuilder.AddPrimaryKey(
				name: "PK_Notifications",
				table: "Notifications",
				column: "Id");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Reverse process: drop PK, drop int column, add Guid column back

			migrationBuilder.DropPrimaryKey(
				name: "PK_Notifications",
				table: "Notifications");

			migrationBuilder.DropColumn(
				name: "Id",
				table: "Notifications");

			migrationBuilder.AddColumn<Guid>(
				name: "Id",
				table: "Notifications",
				type: "uniqueidentifier",
				nullable: false,
				defaultValue: Guid.NewGuid());

			migrationBuilder.AddPrimaryKey(
				name: "PK_Notifications",
				table: "Notifications",
				column: "Id");
		}
	}
}