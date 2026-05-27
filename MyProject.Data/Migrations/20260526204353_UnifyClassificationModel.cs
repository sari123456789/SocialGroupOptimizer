using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyProject.Data.Migrations;

/// <inheritdoc />
public partial class UnifyClassificationModel : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AssignmentClassificationConstraints_Classifications_ClassificationId",
            table: "AssignmentClassificationConstraints");

        migrationBuilder.DropForeignKey(
            name: "FK_ParticipantClassifications_ClassificationAttributes_ClassificationAttributeId",
            table: "ParticipantClassifications");

        migrationBuilder.DropForeignKey(
            name: "FK_ParticipantClassifications_Participants_ParticipantId",
            table: "ParticipantClassifications");

        migrationBuilder.DropTable(
            name: "ParticipantClassifications");

        migrationBuilder.DropTable(
            name: "ClassificationAttributes");

        migrationBuilder.DropPrimaryKey(
            name: "PK_Classifications",
            table: "Classifications");

        migrationBuilder.RenameTable(
            name: "Classifications",
            newName: "ClassificationDimensions");

        migrationBuilder.RenameColumn(
            name: "ClassificationId",
            table: "ClassificationDimensions",
            newName: "ClassificationDimensionId");

        migrationBuilder.RenameColumn(
            name: "ClassificationName",
            table: "ClassificationDimensions",
            newName: "DimensionCode");

        migrationBuilder.AddPrimaryKey(
            name: "PK_ClassificationDimensions",
            table: "ClassificationDimensions",
            column: "ClassificationDimensionId");

        migrationBuilder.CreateTable(
            name: "ClassificationLevels",
            columns: table => new
            {
                ClassificationLevelId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ClassificationDimensionId = table.Column<int>(type: "int", nullable: false),
                LevelCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClassificationLevels", x => x.ClassificationLevelId);
                table.ForeignKey(
                    name: "FK_ClassificationLevels_ClassificationDimensions_ClassificationDimensionId",
                    column: x => x.ClassificationDimensionId,
                    principalTable: "ClassificationDimensions",
                    principalColumn: "ClassificationDimensionId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ClassificationDimensions_DimensionCode",
            table: "ClassificationDimensions",
            column: "DimensionCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ClassificationLevels_ClassificationDimensionId_LevelCode",
            table: "ClassificationLevels",
            columns: new[] { "ClassificationDimensionId", "LevelCode" },
            unique: true);

        migrationBuilder.RenameColumn(
            name: "ClassificationId",
            table: "AssignmentClassificationConstraints",
            newName: "ClassificationDimensionId");

        migrationBuilder.RenameIndex(
            name: "IX_AssignmentClassificationConstraints_ClassificationId",
            table: "AssignmentClassificationConstraints",
            newName: "IX_AssignmentClassificationConstraints_ClassificationDimensionId");

        migrationBuilder.CreateTable(
            name: "ParticipantClassifications",
            columns: table => new
            {
                ParticipantAssignmentId = table.Column<int>(type: "int", nullable: false),
                ClassificationDimensionId = table.Column<int>(type: "int", nullable: false),
                ClassificationLevelId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ParticipantClassifications", x => new { x.ParticipantAssignmentId, x.ClassificationDimensionId });
                table.ForeignKey(
                    name: "FK_ParticipantClassifications_ClassificationDimensions_ClassificationDimensionId",
                    column: x => x.ClassificationDimensionId,
                    principalTable: "ClassificationDimensions",
                    principalColumn: "ClassificationDimensionId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ParticipantClassifications_ClassificationLevels_ClassificationLevelId",
                    column: x => x.ClassificationLevelId,
                    principalTable: "ClassificationLevels",
                    principalColumn: "ClassificationLevelId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ParticipantClassifications_ParticipantAssignments_ParticipantAssignmentId",
                    column: x => x.ParticipantAssignmentId,
                    principalTable: "ParticipantAssignments",
                    principalColumn: "ParticipantAssignmentId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ParticipantClassifications_ClassificationDimensionId",
            table: "ParticipantClassifications",
            column: "ClassificationDimensionId");

        migrationBuilder.CreateIndex(
            name: "IX_ParticipantClassifications_ClassificationLevelId",
            table: "ParticipantClassifications",
            column: "ClassificationLevelId");

        migrationBuilder.AddForeignKey(
            name: "FK_AssignmentClassificationConstraints_ClassificationDimensions_ClassificationDimensionId",
            table: "AssignmentClassificationConstraints",
            column: "ClassificationDimensionId",
            principalTable: "ClassificationDimensions",
            principalColumn: "ClassificationDimensionId",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AssignmentClassificationConstraints_ClassificationDimensions_ClassificationDimensionId",
            table: "AssignmentClassificationConstraints");

        migrationBuilder.DropTable(
            name: "ParticipantClassifications");

        migrationBuilder.DropTable(
            name: "ClassificationLevels");

        migrationBuilder.DropIndex(
            name: "IX_ClassificationDimensions_DimensionCode",
            table: "ClassificationDimensions");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ClassificationDimensions",
            table: "ClassificationDimensions");

        migrationBuilder.RenameTable(
            name: "ClassificationDimensions",
            newName: "Classifications");

        migrationBuilder.RenameColumn(
            name: "ClassificationDimensionId",
            table: "Classifications",
            newName: "ClassificationId");

        migrationBuilder.RenameColumn(
            name: "DimensionCode",
            table: "Classifications",
            newName: "ClassificationName");

        migrationBuilder.AddPrimaryKey(
            name: "PK_Classifications",
            table: "Classifications",
            column: "ClassificationId");

        migrationBuilder.RenameColumn(
            name: "ClassificationDimensionId",
            table: "AssignmentClassificationConstraints",
            newName: "ClassificationId");

        migrationBuilder.RenameIndex(
            name: "IX_AssignmentClassificationConstraints_ClassificationDimensionId",
            table: "AssignmentClassificationConstraints",
            newName: "IX_AssignmentClassificationConstraints_ClassificationId");

        migrationBuilder.CreateTable(
            name: "ClassificationAttributes",
            columns: table => new
            {
                ClassificationAttributeId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ClassificationId = table.Column<int>(type: "int", nullable: false),
                AttributeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClassificationAttributes", x => x.ClassificationAttributeId);
                table.ForeignKey(
                    name: "FK_ClassificationAttributes_Classifications_ClassificationId",
                    column: x => x.ClassificationId,
                    principalTable: "Classifications",
                    principalColumn: "ClassificationId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ParticipantClassifications",
            columns: table => new
            {
                ParticipantId = table.Column<int>(type: "int", nullable: false),
                ClassificationAttributeId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ParticipantClassifications", x => new { x.ParticipantId, x.ClassificationAttributeId });
                table.ForeignKey(
                    name: "FK_ParticipantClassifications_ClassificationAttributes_ClassificationAttributeId",
                    column: x => x.ClassificationAttributeId,
                    principalTable: "ClassificationAttributes",
                    principalColumn: "ClassificationAttributeId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ParticipantClassifications_Participants_ParticipantId",
                    column: x => x.ParticipantId,
                    principalTable: "Participants",
                    principalColumn: "ParticipantId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ClassificationAttributes_ClassificationId_AttributeName",
            table: "ClassificationAttributes",
            columns: new[] { "ClassificationId", "AttributeName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ParticipantClassifications_ClassificationAttributeId",
            table: "ParticipantClassifications",
            column: "ClassificationAttributeId");

        migrationBuilder.AddForeignKey(
            name: "FK_AssignmentClassificationConstraints_Classifications_ClassificationId",
            table: "AssignmentClassificationConstraints",
            column: "ClassificationId",
            principalTable: "Classifications",
            principalColumn: "ClassificationId",
            onDelete: ReferentialAction.Restrict);
    }
}
