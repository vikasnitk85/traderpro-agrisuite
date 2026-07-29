using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlatformFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.CreateTable(
                name: "workspaces",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspaces", x => x.id);
                    table.CheckConstraint("ck_workspaces_status", "status IN (1, 2, 3)");
                });

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trade_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tax_registration_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                    table.UniqueConstraint("ak_companies_workspace_id_id", x => new { x.workspace_id, x.id });
                    table.CheckConstraint("ck_companies_status", "status IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "fk_companies_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalSchema: "platform",
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.id);
                    table.UniqueConstraint("ak_devices_workspace_id_id", x => new { x.workspace_id, x.id });
                    table.CheckConstraint("ck_devices_status", "status IN (1, 2)");
                    table.ForeignKey(
                        name: "fk_devices_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalSchema: "platform",
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    command_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    result_payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_records", x => x.id);
                    table.CheckConstraint("ck_idempotency_records_status", "status IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "fk_idempotency_records_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalSchema: "platform",
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_version = table.Column<int>(type: "integer", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_version = table.Column<long>(type: "bigint", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_messages_aggregate_version", "aggregate_version > 0");
                    table.CheckConstraint("ck_outbox_messages_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_outbox_messages_event_version", "event_version > 0");
                    table.CheckConstraint("ck_outbox_messages_status", "status IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "fk_outbox_messages_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalSchema: "platform",
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.UniqueConstraint("ak_users_workspace_id_id", x => new { x.workspace_id, x.id });
                    table.CheckConstraint("ck_users_status", "status IN (1, 2)");
                    table.ForeignKey(
                        name: "fk_users_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalSchema: "platform",
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branches", x => x.id);
                    table.UniqueConstraint("ak_branches_workspace_id_id", x => new { x.workspace_id, x.id });
                    table.CheckConstraint("ck_branches_status", "status IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "fk_branches_companies_workspace_id_company_id",
                        columns: x => new { x.workspace_id, x.company_id },
                        principalSchema: "platform",
                        principalTable: "companies",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    before_snapshot_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_snapshot_json = table.Column<string>(type: "jsonb", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_events_branches_workspace_id_branch_id",
                        columns: x => new { x.workspace_id, x.branch_id },
                        principalSchema: "platform",
                        principalTable: "branches",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audit_events_companies_workspace_id_company_id",
                        columns: x => new { x.workspace_id, x.company_id },
                        principalSchema: "platform",
                        principalTable: "companies",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audit_events_devices_workspace_id_actor_device_id",
                        columns: x => new { x.workspace_id, x.actor_device_id },
                        principalSchema: "platform",
                        principalTable: "devices",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audit_events_users_workspace_id_actor_user_id",
                        columns: x => new { x.workspace_id, x.actor_user_id },
                        principalSchema: "platform",
                        principalTable: "users",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audit_events_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalSchema: "platform",
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_workspace_id_actor_device_id",
                schema: "platform",
                table: "audit_events",
                columns: new[] { "workspace_id", "actor_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_workspace_id_actor_user_id",
                schema: "platform",
                table: "audit_events",
                columns: new[] { "workspace_id", "actor_user_id" });

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

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_workspace_id_occurred_at_utc",
                schema: "platform",
                table: "audit_events",
                columns: new[] { "workspace_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_branches_workspace_id_company_id_code",
                schema: "platform",
                table: "branches",
                columns: new[] { "workspace_id", "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_branches_workspace_id_company_id_default",
                schema: "platform",
                table: "branches",
                columns: new[] { "workspace_id", "company_id" },
                unique: true,
                filter: "is_default");

            migrationBuilder.CreateIndex(
                name: "ux_companies_workspace_id_code",
                schema: "platform",
                table: "companies",
                columns: new[] { "workspace_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_companies_workspace_id_tax_registration_number",
                schema: "platform",
                table: "companies",
                columns: new[] { "workspace_id", "tax_registration_number" },
                unique: true,
                filter: "tax_registration_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_devices_workspace_id_installation_id",
                schema: "platform",
                table: "devices",
                columns: new[] { "workspace_id", "installation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_idempotency_records_workspace_command_key",
                schema: "platform",
                table: "idempotency_records",
                columns: new[] { "workspace_id", "command_type", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_workspace_status_next_attempt",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "workspace_id", "status", "next_attempt_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_users_workspace_id_username",
                schema: "platform",
                table: "users",
                columns: new[] { "workspace_id", "username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_workspaces_code",
                schema: "platform",
                table: "workspaces",
                column: "code",
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION platform.reject_audit_event_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'platform.audit_events is append-only'
                        USING ERRCODE = '55000';
                END;
                $$;

                CREATE TRIGGER audit_events_append_only
                BEFORE UPDATE OR DELETE ON platform.audit_events
                FOR EACH ROW
                EXECUTE FUNCTION platform.reject_audit_event_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS audit_events_append_only
                    ON platform.audit_events;
                DROP FUNCTION IF EXISTS
                    platform.reject_audit_event_mutation();
                """);

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "idempotency_records",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "branches",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "devices",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "users",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "workspaces",
                schema: "platform");
        }
    }
}
