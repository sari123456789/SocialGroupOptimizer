using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BestSolutionArchives",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BestScore = table.Column<double>(type: "float", nullable: false),
                    SolutionStateBlob = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    WasDiversified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BestSolutionArchives", x => x.RunId);
                });

            migrationBuilder.CreateTable(
                name: "Classifications",
                columns: table => new
                {
                    ClassificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassificationName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classifications", x => x.ClassificationId);
                });

            migrationBuilder.CreateTable(
                name: "Managers",
                columns: table => new
                {
                    ManagerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManagerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Managers", x => x.ManagerId);
                });

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    ParticipantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsraeliIdentityNumber = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    ParticipantName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.ParticipantId);
                });

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
                name: "ManagementGroups",
                columns: table => new
                {
                    ManagementGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManagerId = table.Column<int>(type: "int", nullable: false),
                    ManagementGroupName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagementGroups", x => x.ManagementGroupId);
                    table.ForeignKey(
                        name: "FK_ManagementGroups_Managers_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Managers",
                        principalColumn: "ManagerId",
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

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ManagementGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_Assignments_ManagementGroups_ManagementGroupId",
                        column: x => x.ManagementGroupId,
                        principalTable: "ManagementGroups",
                        principalColumn: "ManagementGroupId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentClassificationConstraints",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    ClassificationId = table.Column<int>(type: "int", nullable: false),
                    IsBalanceOrSeparation = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentClassificationConstraints", x => new { x.AssignmentId, x.ClassificationId });
                    table.ForeignKey(
                        name: "FK_AssignmentClassificationConstraints_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentClassificationConstraints_Classifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "ClassificationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentConflicts",
                columns: table => new
                {
                    AssignmentConflictId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    ConflictType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SystemRecommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentConflicts", x => x.AssignmentConflictId);
                    table.ForeignKey(
                        name: "FK_AssignmentConflicts_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentScoreBreakdowns",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    SocialScore = table.Column<double>(type: "float", nullable: false),
                    BalancePenalty = table.Column<double>(type: "float", nullable: false),
                    IsolationPenalty = table.Column<double>(type: "float", nullable: false),
                    FinalSigma = table.Column<double>(type: "float", nullable: false),
                    ScoreExplanation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentScoreBreakdowns", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_AssignmentScoreBreakdowns_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupCountConstraints",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    MinGroups = table.Column<int>(type: "int", nullable: false),
                    MaxGroups = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupCountConstraints", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_GroupCountConstraints_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupSizeConstraints",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    MinGroupSize = table.Column<int>(type: "int", nullable: false),
                    MaxGroupSize = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupSizeConstraints", x => new { x.AssignmentId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_GroupSizeConstraints_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "ParticipantAssignments",
                columns: table => new
                {
                    ParticipantAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantId = table.Column<int>(type: "int", nullable: false),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    ManagerId = table.Column<int>(type: "int", nullable: false),
                    BalanceContribution = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantAssignments", x => x.ParticipantAssignmentId);
                    table.ForeignKey(
                        name: "FK_ParticipantAssignments_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipantAssignments_Managers_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Managers",
                        principalColumn: "ManagerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantAssignments_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "ParticipantId",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "SocialPreferences",
                columns: table => new
                {
                    FromParticipantAssignmentId = table.Column<int>(type: "int", nullable: false),
                    ToParticipantAssignmentId = table.Column<int>(type: "int", nullable: false),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    PreferenceWeight = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialPreferences", x => new { x.AssignmentId, x.FromParticipantAssignmentId, x.ToParticipantAssignmentId });
                    table.ForeignKey(
                        name: "FK_SocialPreferences_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialPreferences_ParticipantAssignments_FromParticipantAssignmentId",
                        column: x => x.FromParticipantAssignmentId,
                        principalTable: "ParticipantAssignments",
                        principalColumn: "ParticipantAssignmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialPreferences_ParticipantAssignments_ToParticipantAssignmentId",
                        column: x => x.ToParticipantAssignmentId,
                        principalTable: "ParticipantAssignments",
                        principalColumn: "ParticipantAssignmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentClassificationConstraints_ClassificationId",
                table: "AssignmentClassificationConstraints",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentConflicts_AssignmentId",
                table: "AssignmentConflicts",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ManagementGroupId",
                table: "Assignments",
                column: "ManagementGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationAttributes_ClassificationId_AttributeName",
                table: "ClassificationAttributes",
                columns: new[] { "ClassificationId", "AttributeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagementGroups_ManagerId",
                table: "ManagementGroups",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatoryConstraintAssignments_ParticipantAssignmentId",
                table: "MandatoryConstraintAssignments",
                column: "ParticipantAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MandatoryConstraints_AssignmentId",
                table: "MandatoryConstraints",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantAssignments_AssignmentId",
                table: "ParticipantAssignments",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantAssignments_ManagerId",
                table: "ParticipantAssignments",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantAssignments_ParticipantId",
                table: "ParticipantAssignments",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantClassifications_ClassificationAttributeId",
                table: "ParticipantClassifications",
                column: "ClassificationAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_IsraeliIdentityNumber",
                table: "Participants",
                column: "IsraeliIdentityNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialPreferences_FromParticipantAssignmentId",
                table: "SocialPreferences",
                column: "FromParticipantAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialPreferences_ToParticipantAssignmentId",
                table: "SocialPreferences",
                column: "ToParticipantAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentClassificationConstraints");

            migrationBuilder.DropTable(
                name: "AssignmentConflicts");

            migrationBuilder.DropTable(
                name: "AssignmentScoreBreakdowns");

            migrationBuilder.DropTable(
                name: "BestSolutionArchives");

            migrationBuilder.DropTable(
                name: "GroupCountConstraints");

            migrationBuilder.DropTable(
                name: "GroupSizeConstraints");

            migrationBuilder.DropTable(
                name: "MandatoryConstraintAssignments");

            migrationBuilder.DropTable(
                name: "ParticipantClassifications");

            migrationBuilder.DropTable(
                name: "SocialPreferences");

            migrationBuilder.DropTable(
                name: "MandatoryConstraints");

            migrationBuilder.DropTable(
                name: "ClassificationAttributes");

            migrationBuilder.DropTable(
                name: "ParticipantAssignments");

            migrationBuilder.DropTable(
                name: "Classifications");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "ManagementGroups");

            migrationBuilder.DropTable(
                name: "Managers");
        }
    }
}
