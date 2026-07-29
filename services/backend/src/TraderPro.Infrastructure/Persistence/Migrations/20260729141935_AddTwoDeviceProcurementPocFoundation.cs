using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoDeviceProcurementPocFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "procurement");

            migrationBuilder.CreateTable(
                name: "receiving_session_pocs",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cloud_reference_sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    editor_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lease_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_lease_heartbeat_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_expected_local_sequence = table.Column<long>(type: "bigint", nullable: false),
                    entry_count = table.Column<int>(type: "integer", nullable: false),
                    processed_total_weight_kg = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receiving_session_pocs", x => x.id);
                    table.UniqueConstraint("ak_receiving_session_pocs_workspace_id_id", x => new { x.workspace_id, x.id });
                    table.CheckConstraint("ck_receiving_session_pocs_entry_count", "entry_count >= 0");
                    table.CheckConstraint("ck_receiving_session_pocs_lease_shape", "(status = 1\n    AND lease_id IS NOT NULL\n    AND lease_expires_at_utc IS NOT NULL\n    AND last_lease_heartbeat_at_utc IS NOT NULL)\nOR (status <> 1\n    AND lease_id IS NULL\n    AND lease_expires_at_utc IS NULL\n    AND last_lease_heartbeat_at_utc IS NULL)");
                    table.CheckConstraint("ck_receiving_session_pocs_next_sequence", "next_expected_local_sequence > 0");
                    table.CheckConstraint("ck_receiving_session_pocs_reference_sequence", "cloud_reference_sequence > 0");
                    table.CheckConstraint("ck_receiving_session_pocs_status", "status IN (1, 2, 3, 4)");
                    table.CheckConstraint("ck_receiving_session_pocs_total", "processed_total_weight_kg >= 0");
                    table.CheckConstraint("ck_receiving_session_pocs_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_receiving_session_pocs_devices_workspace_approver",
                        columns: x => new { x.workspace_id, x.approved_by_device_id },
                        principalSchema: "platform",
                        principalTable: "devices",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_receiving_session_pocs_devices_workspace_editor",
                        columns: x => new { x.workspace_id, x.editor_device_id },
                        principalSchema: "platform",
                        principalTable: "devices",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_receiving_session_pocs_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalSchema: "platform",
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receiving_entry_pocs",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiving_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    local_sequence = table.Column<long>(type: "bigint", nullable: false),
                    product_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bag_type_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bag_count = table.Column<int>(type: "integer", nullable: false),
                    raw_weight_kg = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    processed_weight_kg = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    display_weight_kg = table.Column<decimal>(type: "numeric(20,3)", precision: 20, scale: 3, nullable: false),
                    decimal_places = table.Column<int>(type: "integer", nullable: false),
                    processing_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    weight_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    captured_at_device_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at_server_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receiving_entry_pocs", x => x.id);
                    table.UniqueConstraint("ak_receiving_entry_pocs_workspace_id_id", x => new { x.workspace_id, x.id });
                    table.CheckConstraint("ck_receiving_entry_pocs_bag_count", "bag_count > 0");
                    table.CheckConstraint("ck_receiving_entry_pocs_decimal_places", "decimal_places IN (1, 2, 3)");
                    table.CheckConstraint("ck_receiving_entry_pocs_display_weight", "display_weight_kg >= 0");
                    table.CheckConstraint("ck_receiving_entry_pocs_local_sequence", "local_sequence > 0");
                    table.CheckConstraint("ck_receiving_entry_pocs_processed_weight", "processed_weight_kg >= 0");
                    table.CheckConstraint("ck_receiving_entry_pocs_processing_method", "processing_method IN ('Standard', 'Floor', 'Ceiling')");
                    table.CheckConstraint("ck_receiving_entry_pocs_weight_source", "weight_source IN ('ManualSpike', 'TestScale')");
                    table.ForeignKey(
                        name: "fk_receiving_entry_pocs_sessions_workspace_session",
                        columns: x => new { x.workspace_id, x.receiving_session_id },
                        principalSchema: "procurement",
                        principalTable: "receiving_session_pocs",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receiving_finalization_pocs",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiving_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    final_entry_count = table.Column<int>(type: "integer", nullable: false),
                    final_processed_total_weight_kg = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    approved_by_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    finalized_by_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    finalized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receiving_finalization_pocs", x => x.id);
                    table.CheckConstraint("ck_receiving_finalization_pocs_entry_count", "final_entry_count > 0");
                    table.CheckConstraint("ck_receiving_finalization_pocs_total", "final_processed_total_weight_kg >= 0");
                    table.CheckConstraint("ck_receiving_finalization_pocs_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_receiving_finalization_pocs_devices_workspace_approver",
                        columns: x => new { x.workspace_id, x.approved_by_device_id },
                        principalSchema: "platform",
                        principalTable: "devices",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_receiving_finalization_pocs_devices_workspace_finalizer",
                        columns: x => new { x.workspace_id, x.finalized_by_device_id },
                        principalSchema: "platform",
                        principalTable: "devices",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_receiving_finalization_pocs_sessions_workspace_session",
                        columns: x => new { x.workspace_id, x.receiving_session_id },
                        principalSchema: "procurement",
                        principalTable: "receiving_session_pocs",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_receiving_entry_pocs_workspace_operation",
                schema: "procurement",
                table: "receiving_entry_pocs",
                columns: new[] { "workspace_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_receiving_entry_pocs_workspace_session_sequence",
                schema: "procurement",
                table: "receiving_entry_pocs",
                columns: new[] { "workspace_id", "receiving_session_id", "local_sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receiving_finalization_pocs_workspace_id_approved_by_device~",
                schema: "procurement",
                table: "receiving_finalization_pocs",
                columns: new[] { "workspace_id", "approved_by_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_receiving_finalization_pocs_workspace_id_finalized_by_devic~",
                schema: "procurement",
                table: "receiving_finalization_pocs",
                columns: new[] { "workspace_id", "finalized_by_device_id" });

            migrationBuilder.CreateIndex(
                name: "ux_receiving_finalization_pocs_workspace_session",
                schema: "procurement",
                table: "receiving_finalization_pocs",
                columns: new[] { "workspace_id", "receiving_session_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receiving_session_pocs_workspace_id_approved_by_device_id",
                schema: "procurement",
                table: "receiving_session_pocs",
                columns: new[] { "workspace_id", "approved_by_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_receiving_session_pocs_workspace_id_editor_device_id",
                schema: "procurement",
                table: "receiving_session_pocs",
                columns: new[] { "workspace_id", "editor_device_id" });

            migrationBuilder.CreateIndex(
                name: "ix_receiving_session_pocs_workspace_status_reference",
                schema: "procurement",
                table: "receiving_session_pocs",
                columns: new[] { "workspace_id", "status", "cloud_reference_sequence" });

            migrationBuilder.CreateIndex(
                name: "ix_receiving_session_pocs_workspace_updated_at",
                schema: "procurement",
                table: "receiving_session_pocs",
                columns: new[] { "workspace_id", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_receiving_session_pocs_cloud_reference_sequence",
                schema: "procurement",
                table: "receiving_session_pocs",
                column: "cloud_reference_sequence",
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION procurement.reject_receiving_poc_immutable_row()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    RAISE EXCEPTION
                        'Procurement POC immutable rows cannot be changed or deleted.'
                        USING ERRCODE = '55000';
                END;
                $function$;

                CREATE TRIGGER tr_receiving_entry_pocs_immutable
                BEFORE UPDATE OR DELETE
                ON procurement.receiving_entry_pocs
                FOR EACH ROW
                EXECUTE FUNCTION procurement.reject_receiving_poc_immutable_row();

                CREATE TRIGGER tr_receiving_finalization_pocs_immutable
                BEFORE UPDATE OR DELETE
                ON procurement.receiving_finalization_pocs
                FOR EACH ROW
                EXECUTE FUNCTION procurement.reject_receiving_poc_immutable_row();

                CREATE FUNCTION procurement.reject_receiving_session_poc_workspace_change()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF NEW.workspace_id IS DISTINCT FROM OLD.workspace_id THEN
                        RAISE EXCEPTION
                            'Receiving Session POC workspace ownership is immutable.'
                            USING ERRCODE = '55000';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER tr_receiving_session_pocs_workspace_immutable
                BEFORE UPDATE OF workspace_id
                ON procurement.receiving_session_pocs
                FOR EACH ROW
                EXECUTE FUNCTION procurement.reject_receiving_session_poc_workspace_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS tr_receiving_session_pocs_workspace_immutable
                    ON procurement.receiving_session_pocs;
                DROP FUNCTION IF EXISTS
                    procurement.reject_receiving_session_poc_workspace_change();
                DROP TRIGGER IF EXISTS tr_receiving_finalization_pocs_immutable
                    ON procurement.receiving_finalization_pocs;
                DROP TRIGGER IF EXISTS tr_receiving_entry_pocs_immutable
                    ON procurement.receiving_entry_pocs;
                DROP FUNCTION IF EXISTS
                    procurement.reject_receiving_poc_immutable_row();
                """);

            migrationBuilder.DropTable(
                name: "receiving_entry_pocs",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "receiving_finalization_pocs",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "receiving_session_pocs",
                schema: "procurement");
        }
    }
}
