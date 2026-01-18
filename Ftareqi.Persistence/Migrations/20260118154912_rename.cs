using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ftareqi.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class rename : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// Rename column 'palette' to 'Plate' in 'Cars' table
			migrationBuilder.Sql("EXEC sp_rename 'dbo.Car.palette', 'Plate', 'COLUMN';");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Rollback: rename 'Plate' back to 'palette '
			migrationBuilder.Sql("EXEC sp_rename 'dbo.Car.Plate', 'palette', 'COLUMN';");
		}
	}
}
