using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenPlatformFoundationIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_audit_events_branches_workspace_id_branch_id",
                schema: "platform",
                table: "audit_events");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_branches_workspace_id_id",
                schema: "platform",
                table: "branches");

            migrationBuilder.DropIndex(
                name: "IX_audit_events_workspace_id_branch_id",
                schema: "platform",
                table: "audit_events");

            migrationBuilder.DropIndex(
                name: "IX_audit_events_workspace_id_company_id",
                schema: "platform",
                table: "audit_events");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_branches_workspace_id_company_id_id",
                schema: "platform",
                table: "branches",
                columns: new[] { "workspace_id", "company_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_workspace_aggregate_history",
                schema: "platform",
                table: "audit_events",
                columns: new[] { "workspace_id", "aggregate_type", "aggregate_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_workspace_id_company_id_branch_id",
                schema: "platform",
                table: "audit_events",
                columns: new[] { "workspace_id", "company_id", "branch_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_audit_events_branch_requires_company",
                schema: "platform",
                table: "audit_events",
                sql: "branch_id IS NULL OR company_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_audit_events_branches_workspace_company_branch",
                schema: "platform",
                table: "audit_events",
                columns: new[] { "workspace_id", "company_id", "branch_id" },
                principalSchema: "platform",
                principalTable: "branches",
                principalColumns: new[] { "workspace_id", "company_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_audit_events_branches_workspace_company_branch",
                schema: "platform",
                table: "audit_events");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_branches_workspace_id_company_id_id",
                schema: "platform",
                table: "branches");

            migrationBuilder.DropIndex(
                name: "ix_audit_events_workspace_aggregate_history",
                schema: "platform",
                table: "audit_events");

            migrationBuilder.DropIndex(
                name: "IX_audit_events_workspace_id_company_id_branch_id",
                schema: "platform",
                table: "audit_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_audit_events_branch_requires_company",
                schema: "platform",
                table: "audit_events");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_branches_workspace_id_id",
                schema: "platform",
                table: "branches",
                columns: new[] { "workspace_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_workspace_id_branch_id",
                schema: "platform",
                table: "audit_events",
                columns: new[] { "workspace_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_workspace_id_company_id",
                schema: "platform",
                table: "audit_events",
                columns: new[] { "workspace_id", "company_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_audit_events_branches_workspace_id_branch_id",
                schema: "platform",
                table: "audit_events",
                columns: new[] { "workspace_id", "branch_id" },
                principalSchema: "platform",
                principalTable: "branches",
                principalColumns: new[] { "workspace_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
