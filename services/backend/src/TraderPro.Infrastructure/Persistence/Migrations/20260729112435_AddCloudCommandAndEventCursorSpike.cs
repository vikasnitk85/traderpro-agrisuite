using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudCommandAndEventCursorSpike : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "sequence",
                schema: "platform",
                table: "outbox_messages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);

            migrationBuilder.AddColumn<int>(
                name: "result_status_code",
                schema: "platform",
                table: "idempotency_records",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "command_probes",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    counter = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_command_probes", x => x.id);
                    table.UniqueConstraint("ak_command_probes_workspace_id_id", x => new { x.workspace_id, x.id });
                    table.CheckConstraint("ck_command_probes_counter", "counter >= 0");
                    table.CheckConstraint("ck_command_probes_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_command_probes_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalSchema: "platform",
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_workspace_id_sequence",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "workspace_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ux_outbox_messages_sequence",
                schema: "platform",
                table: "outbox_messages",
                column: "sequence",
                unique: true);

            migrationBuilder.Sql(
                """
                UPDATE platform.idempotency_records
                SET result_payload_json = COALESCE(result_payload_json, '{}'::jsonb),
                    result_status_code = 200
                WHERE status = 2;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_idempotency_records_result_status_code",
                schema: "platform",
                table: "idempotency_records",
                sql: "(status = 2\n    AND result_payload_json IS NOT NULL\n    AND result_status_code BETWEEN 100 AND 599)\nOR (status <> 2 AND result_status_code IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ux_command_probes_workspace_id_name",
                schema: "platform",
                table: "command_probes",
                columns: new[] { "workspace_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "command_probes",
                schema: "platform");

            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_workspace_id_sequence",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "ux_outbox_messages_sequence",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_idempotency_records_result_status_code",
                schema: "platform",
                table: "idempotency_records");

            migrationBuilder.DropColumn(
                name: "sequence",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "result_status_code",
                schema: "platform",
                table: "idempotency_records");
        }
    }
}
