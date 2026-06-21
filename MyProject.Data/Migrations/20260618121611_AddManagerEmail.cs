using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Managers_ManagerName",
                table: "Managers");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Managers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE Managers
                SET Email = CONCAT('manager', ManagerId, '@legacy.local')
                WHERE Email IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE Managers
                SET Email = 'demo@myproject.local'
                WHERE ManagerName = N'מנהל דמו';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Managers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Managers_Email",
                table: "Managers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Managers_Email",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Managers");

            migrationBuilder.CreateIndex(
                name: "IX_Managers_ManagerName",
                table: "Managers",
                column: "ManagerName",
                unique: true);
        }
    }
}
