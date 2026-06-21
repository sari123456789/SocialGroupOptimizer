using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlacementScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LastInitialPlacementScore",
                table: "Assignments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastPlacementScore",
                table: "Assignments",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastInitialPlacementScore",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "LastPlacementScore",
                table: "Assignments");
        }
    }
}
