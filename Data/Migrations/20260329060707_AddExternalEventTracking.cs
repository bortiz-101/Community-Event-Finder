using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Community_Event_Finder.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalEventTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalEventId",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalEventSourceType",
                table: "Events",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalEventId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ExternalEventSourceType",
                table: "Events");
        }
    }
}
