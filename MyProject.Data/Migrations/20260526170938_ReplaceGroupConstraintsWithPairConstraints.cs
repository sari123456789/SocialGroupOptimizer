using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceGroupConstraintsWithPairConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MandatoryConstraintAssignments");

            migrationBuilder.DropTable(
                name: "MandatoryConstraints");

            migrationBuilder.CreateTable(
                name: "ForbiddenPairConstraints",
                columns: table => new
                {
                    ForbiddenPairConstraintId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    FirstParticipantAssignmentId = table.Column<int>(type: "int", nullable: false),
                    SecondParticipantAssignmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForbiddenPairConstraints", x => x.ForbiddenPairConstraintId);
                    table.ForeignKey(
                        name: "FK_ForbiddenPairConstraints_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForbiddenPairConstraints_ParticipantAssignments_FirstParticipantAssignmentId",
                        column: x => x.FirstParticipantAssignmentId,
                        principalTable: "ParticipantAssignments",
                        principalColumn: "ParticipantAssignmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ForbiddenPairConstraints_ParticipantAssignments_SecondParticipantAssignmentId",
                        column: x => x.SecondParticipantAssignmentId,
                        principalTable: "ParticipantAssignments",
                        principalColumn: "ParticipantAssignmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MandatoryPairConstraints",
                columns: table => new
                {
                    MandatoryPairConstraintId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    FirstParticipantAssignmentId = table.Column<int>(type: "int", nullable: false),
                    SecondParticipantAssignmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandatoryPairConstraints", x => x.MandatoryPairConstraintId);
                    table.ForeignKey(
                        name: "FK_MandatoryPairConstraints_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MandatoryPairConstraints_ParticipantAssignments_FirstParticipantAssignmentId",
                        column: x => x.FirstParticipantAssignmentId,
                        principalTable: "ParticipantAssignments",
                        principalColumn: "ParticipantAssignmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MandatoryPairConstraints_ParticipantAssignments_SecondParticipantAssignmentId",
                        column: x => x.SecondParticipantAssignmentId,
                        principalTable: "ParticipantAssignments",
                        principalColumn: "ParticipantAssignmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForbiddenPairConstraints_AssignmentId",
                table: "ForbiddenPairConstraints",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ForbiddenPairConstraints_FirstParticipantAssignmentId",
                table: "ForbiddenPairConstraints",
                column: "FirstParticipantAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ForbiddenPairConstraints_SecondParticipantAssignmentId",
                table: "ForbiddenPairConstraints",
                column: "SecondParticipantAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatoryPairConstraints_AssignmentId",
                table: "MandatoryPairConstraints",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatoryPairConstraints_FirstParticipantAssignmentId",
                table: "MandatoryPairConstraints",
                column: "FirstParticipantAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatoryPairConstraints_SecondParticipantAssignmentId",
                table: "MandatoryPairConstraints",
                column: "SecondParticipantAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForbiddenPairConstraints");

            migrationBuilder.DropTable(
                name: "MandatoryPairConstraints");

            migrationBuilder.CreateTable(
                name: "MandatoryConstraints",
                columns: table => new
                {
                    MandatoryConstraintId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    ConstraintName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandatoryConstraints", x => x.MandatoryConstraintId);
                    table.ForeignKey(
                        name: "FK_MandatoryConstraints_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MandatoryConstraintAssignments",
                columns: table => new
                {
                    MandatoryConstraintId = table.Column<int>(type: "int", nullable: false),
                    ParticipantAssignmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandatoryConstraintAssignments", x => new { x.MandatoryConstraintId, x.ParticipantAssignmentId });
                    table.ForeignKey(
                        name: "FK_MandatoryConstraintAssignments_MandatoryConstraints_MandatoryConstraintId",
                        column: x => x.MandatoryConstraintId,
                        principalTable: "MandatoryConstraints",
                        principalColumn: "MandatoryConstraintId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MandatoryConstraintAssignments_ParticipantAssignments_ParticipantAssignmentId",
                        column: x => x.ParticipantAssignmentId,
                        principalTable: "ParticipantAssignments",
                        principalColumn: "ParticipantAssignmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MandatoryConstraintAssignments_ParticipantAssignmentId",
                table: "MandatoryConstraintAssignments",
                column: "ParticipantAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatoryConstraints_AssignmentId",
                table: "MandatoryConstraints",
                column: "AssignmentId");
        }
    }
}
