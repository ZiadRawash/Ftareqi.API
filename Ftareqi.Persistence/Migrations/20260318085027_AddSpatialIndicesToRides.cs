using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ftareqi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpatialIndicesToRides : Migration
    {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("CREATE SPATIAL INDEX IX_Rides_StartLocation ON Rides(StartLocation);");

			migrationBuilder.Sql("CREATE SPATIAL INDEX IX_Rides_EndLocation ON Rides(EndLocation);");
		}
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("DROP INDEX IX_Rides_StartLocation ON Rides;");
			migrationBuilder.Sql("DROP INDEX IX_Rides_EndLocation ON Rides;");
		}
	}
}
