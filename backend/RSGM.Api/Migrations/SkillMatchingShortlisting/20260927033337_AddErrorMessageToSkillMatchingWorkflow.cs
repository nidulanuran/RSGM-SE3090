using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSGM.Api.Migrations.SkillMatchingShortlisting
{
    /// <inheritdoc />
    public partial class AddErrorMessageToSkillMatchingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SkillMatchingAgentWorkflows_JobPostingId_CreatedAt",
                table: "SkillMatchingAgentWorkflows");

            migrationBuilder.DropIndex(
                name: "IX_SkillMatchingAgentWorkflows_JobPostingId_Status",
                table: "SkillMatchingAgentWorkflows");

            migrationBuilder.RenameColumn(
                name: "FailureReason",
                table: "SkillMatchingAgentWorkflows",
                newName: "ErrorMessage");

            migrationBuilder.AlterColumn<string>(
                name: "InputSummary",
                table: "SkillMatchingAgentWorkflowSteps",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "RecommendationJson",
                table: "SkillMatchingAgentWorkflows",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlanJson",
                table: "SkillMatchingAgentWorkflows",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillMatchingAgentWorkflows_JobPostingId",
                table: "SkillMatchingAgentWorkflows",
                column: "JobPostingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SkillMatchingAgentWorkflows_JobPostingId",
                table: "SkillMatchingAgentWorkflows");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "SkillMatchingAgentWorkflows",
                newName: "FailureReason");

            migrationBuilder.AlterColumn<string>(
                name: "InputSummary",
                table: "SkillMatchingAgentWorkflowSteps",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "RecommendationJson",
                table: "SkillMatchingAgentWorkflows",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PlanJson",
                table: "SkillMatchingAgentWorkflows",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_SkillMatchingAgentWorkflows_JobPostingId_CreatedAt",
                table: "SkillMatchingAgentWorkflows",
                columns: new[] { "JobPostingId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillMatchingAgentWorkflows_JobPostingId_Status",
                table: "SkillMatchingAgentWorkflows",
                columns: new[] { "JobPostingId", "Status" });
        }
    }
}
