using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSGM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHrAgenticWorkflowTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HrAgentWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Objective = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    WorkflowType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    PlanJson = table.Column<string>(type: "text", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalOutcome = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrAgentWorkflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrAgentWorkflows_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MetadataSummary = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrAuditLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HrAgentWorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    AgentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ToolName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InputSummary = table.Column<string>(type: "text", nullable: false),
                    OutputSummary = table.Column<string>(type: "text", nullable: false),
                    ValidationStatus = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrAgentWorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrAgentWorkflowSteps_HrAgentWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "HrAgentWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HrApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredApproverRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssignedApproverUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    DecisionComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrApprovalRequests_AspNetUsers_AssignedApproverUserId",
                        column: x => x.AssignedApproverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrApprovalRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrApprovalRequests_HrAgentWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "HrAgentWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HrAgentWorkflows_CreatedByUserId",
                table: "HrAgentWorkflows",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HrAgentWorkflows_EntityId",
                table: "HrAgentWorkflows",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_HrAgentWorkflows_Status",
                table: "HrAgentWorkflows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HrAgentWorkflowSteps_StepNumber",
                table: "HrAgentWorkflowSteps",
                column: "StepNumber");

            migrationBuilder.CreateIndex(
                name: "IX_HrAgentWorkflowSteps_WorkflowId",
                table: "HrAgentWorkflowSteps",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_HrApprovalRequests_AssignedApproverUserId",
                table: "HrApprovalRequests",
                column: "AssignedApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HrApprovalRequests_Decision",
                table: "HrApprovalRequests",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_HrApprovalRequests_EntityId",
                table: "HrApprovalRequests",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_HrApprovalRequests_RequestedByUserId",
                table: "HrApprovalRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HrApprovalRequests_WorkflowId",
                table: "HrApprovalRequests",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_HrAuditLogs_Action",
                table: "HrAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_HrAuditLogs_EntityId",
                table: "HrAuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_HrAuditLogs_Timestamp",
                table: "HrAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_HrAuditLogs_UserId",
                table: "HrAuditLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HrAgentWorkflowSteps");

            migrationBuilder.DropTable(
                name: "HrApprovalRequests");

            migrationBuilder.DropTable(
                name: "HrAuditLogs");

            migrationBuilder.DropTable(
                name: "HrAgentWorkflows");
        }
    }
}
