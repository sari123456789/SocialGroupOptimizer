using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentValidationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastPlacementGroupsJson",
                table: "Assignments",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastPlacementStatus",
                table: "Assignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastValidatedAtUtc",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastValidationErrors",
                table: "Assignments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationStatus",
                table: "Assignments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "PendingValidation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPlacementGroupsJson",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "LastPlacementStatus",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "LastValidatedAtUtc",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "LastValidationErrors",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ValidationStatus",
                table: "Assignments");
        }
    }
}
