using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Community_Event_Finder.Migrations
{
    /// <inheritdoc />
    public partial class FixCoordinatePrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Locations",
                type: "decimal(13,8)",
                precision: 13,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,8)",
                oldPrecision: 10,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Locations",
                type: "decimal(13,8)",
                precision: 13,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,8)",
                oldPrecision: 10,
                oldScale: 8,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Locations",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(13,8)",
                oldPrecision: 13,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Locations",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(13,8)",
                oldPrecision: 13,
                oldScale: 8,
                oldNullable: true);
        }
    }
}
