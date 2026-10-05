using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RSGM.Api.Data;

#nullable disable

namespace RSGM.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260923130000_AddAgentWorkflow")]
public partial class AddAgentWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AgentWorkflows",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                InitiatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ReadinessStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                WorkflowEligible = table.Column<bool>(type: "boolean", nullable: false),
                SourceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                WarningsJson = table.Column<string>(type: "jsonb", nullable: false),
                StepsJson = table.Column<string>(type: "jsonb", nullable: false),
                NextStep = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PolicyVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_AgentWorkflows", x => x.Id);
                table.ForeignKey("FK_AgentWorkflows_Applications_ApplicationId", x => x.ApplicationId,
                    "Applications", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_AgentWorkflows_AspNetUsers_InitiatedByUserId", x => x.InitiatedByUserId,
                    "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_AgentWorkflows_ApplicationId_StartedAt",
            table: "AgentWorkflows", columns: new[] { "ApplicationId", "StartedAt" });
        migrationBuilder.CreateIndex(name: "IX_AgentWorkflows_InitiatedByUserId",
            table: "AgentWorkflows", column: "InitiatedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "AgentWorkflows");
}
