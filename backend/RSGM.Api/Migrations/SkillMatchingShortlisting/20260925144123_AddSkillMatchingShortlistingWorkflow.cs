using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSGM.Api.Migrations.SkillMatchingShortlisting
{
    /// <inheritdoc />
    public partial class AddSkillMatchingShortlistingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkillMatchingAgentWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPostingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecruiterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PanelistId = table.Column<Guid>(type: "uuid", nullable: false),
                    Objective = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PlanJson = table.Column<string>(type: "text", nullable: true),
                    RecommendationJson = table.Column<string>(type: "text", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DispatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionComment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillMatchingAgentWorkflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillMatchingAgentWorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    AgentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InputSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    OutputSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillMatchingAgentWorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillMatchingAgentWorkflowSteps_SkillMatchingAgentWorkflows~",
                        column: x => x.WorkflowId,
                        principalTable: "SkillMatchingAgentWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillMatchingAgentWorkflows_JobPostingId_CreatedAt",
                table: "SkillMatchingAgentWorkflows",
                columns: new[] { "JobPostingId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillMatchingAgentWorkflows_JobPostingId_Status",
                table: "SkillMatchingAgentWorkflows",
                columns: new[] { "JobPostingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillMatchingAgentWorkflows_RecruiterId_CreatedAt",
                table: "SkillMatchingAgentWorkflows",
                columns: new[] { "RecruiterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillMatchingAgentWorkflowSteps_WorkflowId_StepNumber",
                table: "SkillMatchingAgentWorkflowSteps",
                columns: new[] { "WorkflowId", "StepNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkillMatchingAgentWorkflowSteps");

            migrationBuilder.DropTable(
                name: "SkillMatchingAgentWorkflows");
        }
    }
}
