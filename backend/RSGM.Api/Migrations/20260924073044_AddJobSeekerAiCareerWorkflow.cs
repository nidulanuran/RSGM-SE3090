using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSGM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSeekerAiCareerWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobSeekerAiWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Objective = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrentStep = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PlanJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProfileAnalysisJson = table.Column<string>(type: "jsonb", nullable: false),
                    JobMatchesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CareerAdviceJson = table.Column<string>(type: "jsonb", nullable: false),
                    ValidationJson = table.Column<string>(type: "jsonb", nullable: false),
                    StepsJson = table.Column<string>(type: "jsonb", nullable: false),
                    SelectedJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ApprovalComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalOutcomeJson = table.Column<string>(type: "jsonb", nullable: false),
                    ErrorSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PolicyVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSeekerAiWorkflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobSeekerAiWorkflows_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobSeekerAiWorkflows_Status_ApprovalStatus",
                table: "JobSeekerAiWorkflows",
                columns: new[] { "Status", "ApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_JobSeekerAiWorkflows_UserId_StartedAt",
                table: "JobSeekerAiWorkflows",
                columns: new[] { "UserId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobSeekerAiWorkflows");
        }
    }
}
