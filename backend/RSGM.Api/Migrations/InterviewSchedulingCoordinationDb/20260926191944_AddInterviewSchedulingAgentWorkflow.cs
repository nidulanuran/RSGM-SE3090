using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSGM.Api.Migrations.InterviewSchedulingCoordinationDb
{
    /// <inheritdoc />
    public partial class AddInterviewSchedulingAgentWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewSchedulingAgentWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPostingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    PanelistId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecruiterId = table.Column<Guid>(type: "uuid", nullable: false),
                    HrManagerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Objective = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlanJson = table.Column<string>(type: "text", nullable: false),
                    AvailableSlotsJson = table.Column<string>(type: "text", nullable: false),
                    InterviewType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    LocationOrLink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProposalJson = table.Column<string>(type: "text", nullable: true),
                    InterviewId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModeApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModeApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduleApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionComment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewSchedulingAgentWorkflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewSchedulingAgentWorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    AgentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InputSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OutputSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewSchedulingAgentWorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewSchedulingAgentWorkflowSteps_InterviewSchedulingAg~",
                        column: x => x.WorkflowId,
                        principalTable: "InterviewSchedulingAgentWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedulingAgentWorkflows_ApplicationId",
                table: "InterviewSchedulingAgentWorkflows",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedulingAgentWorkflows_JobPostingId",
                table: "InterviewSchedulingAgentWorkflows",
                column: "JobPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedulingAgentWorkflows_PanelistId_CreatedAt",
                table: "InterviewSchedulingAgentWorkflows",
                columns: new[] { "PanelistId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedulingAgentWorkflowSteps_WorkflowId_StepNumber",
                table: "InterviewSchedulingAgentWorkflowSteps",
                columns: new[] { "WorkflowId", "StepNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewSchedulingAgentWorkflowSteps");

            migrationBuilder.DropTable(
                name: "InterviewSchedulingAgentWorkflows");
        }
    }
}
