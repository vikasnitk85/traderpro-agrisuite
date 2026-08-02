using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialReceivingBackend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_outbox_messages_event_stream",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.EnsureSchema(
                name: "sync");

            migrationBuilder.CreateTable(
                name: "commercial_outbox_audiences",
                schema: "platform",
                columns: table => new
                {
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    audience = table.Column<short>(type: "smallint", nullable: false),
                    target_device_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commercial_outbox_audiences", x => x.outbox_message_id);
                    table.CheckConstraint("ck_commercial_outbox_audiences_shape", "audience IN (1, 2) AND ((audience = 1 AND target_device_id IS NULL) OR (audience = 2 AND target_device_id IS NOT NULL))");
                    table.ForeignKey(
                        name: "fk_commercial_outbox_audiences_company",
                        columns: x => new { x.workspace_id, x.company_id },
                        principalSchema: "platform",
                        principalTable: "companies",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commercial_outbox_audiences_message",
                        column: x => x.outbox_message_id,
                        principalSchema: "platform",
                        principalTable: "outbox_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commercial_outbox_audiences_target_device",
                        columns: x => new { x.workspace_id, x.target_device_id },
                        principalSchema: "platform",
                        principalTable: "devices",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_master_changes",
                schema: "sync",
                columns: table => new
                {
                    sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    master_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    master_id = table.Column<Guid>(type: "uuid", nullable: false),
                    master_version = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commercial_master_changes", x => x.sequence);
                    table.CheckConstraint("ck_commercial_master_changes_shape", "master_version > 0 AND status IN ('Active', 'Inactive')");
                    table.CheckConstraint("ck_commercial_master_changes_type", "master_type IN ('CompanyProcurementSettings', 'BusinessLocation', 'ReceivingVehicle', 'BagType', 'WeightProcessingPolicy', 'Supplier', 'SupplierProductScope', 'ProductGroup', 'Product', 'ProductStandardBagWeight')");
                });

            migrationBuilder.CreateTable(
                name: "commercial_receiving_reference_policies",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    format_template = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reset_policy = table.Column<short>(type: "smallint", nullable: false),
                    starting_number = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commercial_receiving_reference_policies", x => x.id);
                    table.UniqueConstraint("ak_commercial_receiving_reference_policies_scope_id", x => new { x.workspace_id, x.company_id, x.id });
                    table.CheckConstraint("ck_commercial_receiving_reference_policies_document_type", "document_type = 'CommercialReceiving'");
                    table.CheckConstraint("ck_commercial_receiving_reference_policies_reset_policy", "reset_policy IN (1, 2, 3)");
                    table.CheckConstraint("ck_commercial_receiving_reference_policies_starting_number", "starting_number > 0");
                    table.CheckConstraint("ck_commercial_receiving_reference_policies_template", "char_length(format_template) BETWEEN 1 AND 100 AND format_template = btrim(format_template) AND regexp_count(format_template, '\\{SEQ:0{1,12}\\}') = 1 AND regexp_replace(format_template, '\\{SEQ:0{1,12}\\}|\\{YYYY\\}|\\{MM\\}', '', 'g') !~ '[{}[:cntrl:]]' AND (reset_policy <> 2 OR strpos(format_template, '{YYYY}') > 0) AND (reset_policy <> 3 OR (strpos(format_template, '{YYYY}') > 0 AND strpos(format_template, '{MM}') > 0))");
                    table.CheckConstraint("ck_commercial_receiving_reference_policies_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_commercial_receiving_reference_policies_company",
                        columns: x => new { x.workspace_id, x.company_id },
                        principalSchema: "platform",
                        principalTable: "companies",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_receiving_sessions",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cloud_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cloud_reference_sequence = table.Column<long>(type: "bigint", nullable: false),
                    reference_policy_version_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    external_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_version_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    supplier_code_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    supplier_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier_product_scope_mode_snapshot = table.Column<short>(type: "smallint", nullable: false),
                    company_procurement_settings_id = table.Column<Guid>(type: "uuid", nullable: false),
                    procurement_settings_version_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    vehicle_selection_mode_snapshot = table.Column<short>(type: "smallint", nullable: false),
                    destination_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_location_version_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    destination_location_code_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    destination_location_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    weight_processing_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight_policy_version_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    weight_decimal_places_snapshot = table.Column<int>(type: "integer", nullable: false),
                    weight_processing_method_snapshot = table.Column<short>(type: "smallint", nullable: false),
                    receiving_vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    receiving_vehicle_version_snapshot = table.Column<long>(type: "bigint", nullable: true),
                    vehicle_code_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    vehicle_registration_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    vehicle_display_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    next_expected_local_sequence = table.Column<long>(type: "bigint", nullable: false),
                    entry_count = table.Column<int>(type: "integer", nullable: false),
                    processed_total_weight_kg = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    started_at_device_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_server_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commercial_receiving_sessions", x => x.id);
                    table.UniqueConstraint("ak_commercial_receiving_sessions_scope_id", x => new { x.workspace_id, x.company_id, x.id });
                    table.CheckConstraint("ck_commercial_receiving_sessions_shape", "version > 0 AND next_expected_local_sequence >= 2 AND entry_count >= 0 AND processed_total_weight_kg >= 0 AND cloud_reference_sequence > 0 AND reference_policy_version_snapshot > 0 AND ((status = 1 AND submitted_at_utc IS NULL) OR (status = 2 AND submitted_at_utc IS NOT NULL AND entry_count > 0 AND processed_total_weight_kg > 0))");
                    table.CheckConstraint("ck_commercial_receiving_sessions_snapshots", "supplier_version_snapshot > 0 AND supplier_product_scope_mode_snapshot IN (1, 2) AND procurement_settings_version_snapshot > 0 AND vehicle_selection_mode_snapshot IN (1, 2) AND destination_location_version_snapshot > 0 AND weight_policy_version_snapshot > 0 AND weight_decimal_places_snapshot BETWEEN 1 AND 3 AND weight_processing_method_snapshot IN (0, 1, 2)");
                    table.CheckConstraint("ck_commercial_receiving_sessions_status", "status IN (1, 2)");
                    table.CheckConstraint("ck_commercial_receiving_sessions_vehicle_shape", "(receiving_vehicle_id IS NULL AND receiving_vehicle_version_snapshot IS NULL AND vehicle_code_snapshot IS NULL AND vehicle_registration_snapshot IS NULL) OR (receiving_vehicle_id IS NOT NULL AND receiving_vehicle_version_snapshot > 0 AND vehicle_code_snapshot IS NOT NULL AND vehicle_registration_snapshot IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_commercial_receiving_sessions_branch",
                        columns: x => new { x.workspace_id, x.company_id, x.branch_id },
                        principalSchema: "platform",
                        principalTable: "branches",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_receiving_reference_counters",
                schema: "procurement",
                columns: table => new
                {
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    next_number = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commercial_receiving_reference_counters", x => new { x.workspace_id, x.company_id, x.policy_id, x.period_key });
                    table.CheckConstraint("ck_commercial_receiving_reference_counters_next", "next_number > 0");
                    table.CheckConstraint("ck_commercial_receiving_reference_counters_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_commercial_receiving_reference_counters_policy",
                        columns: x => new { x.workspace_id, x.company_id, x.policy_id },
                        principalSchema: "procurement",
                        principalTable: "commercial_receiving_reference_policies",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_receiving_entries",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiving_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    local_sequence = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_version_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    product_code_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    product_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    product_type_snapshot = table.Column<short>(type: "smallint", nullable: false),
                    processing_family_code_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    supplier_product_scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supplier_product_scope_version_snapshot = table.Column<long>(type: "bigint", nullable: true),
                    supplier_scope_mode_snapshot = table.Column<short>(type: "smallint", nullable: false),
                    supplier_scope_validation_result_snapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    bag_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bag_type_version_snapshot = table.Column<long>(type: "bigint", nullable: false),
                    bag_type_code_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    bag_type_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bag_construction_class_snapshot = table.Column<short>(type: "smallint", nullable: false),
                    bag_tare_weight_kg_snapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    bag_returnable_snapshot = table.Column<bool>(type: "boolean", nullable: false),
                    product_standard_bag_weight_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_standard_bag_weight_version_snapshot = table.Column<long>(type: "bigint", nullable: true),
                    standard_bag_weight_label_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    standard_content_weight_kg_snapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    bag_count = table.Column<int>(type: "integer", nullable: false),
                    raw_weight_kg = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    processed_weight_kg = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    display_weight_kg = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    decimal_places_snapshot = table.Column<int>(type: "integer", nullable: false),
                    processing_method_snapshot = table.Column<short>(type: "smallint", nullable: false),
                    weight_source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    captured_at_device_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at_server_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commercial_receiving_entries", x => x.id);
                    table.CheckConstraint("ck_commercial_receiving_entries_physical", "local_sequence > 1 AND bag_count > 0 AND processed_weight_kg > 0 AND decimal_places_snapshot BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_commercial_receiving_entries_snapshots", "product_version_snapshot > 0 AND product_type_snapshot IN (1, 2, 3, 4, 5) AND supplier_scope_mode_snapshot IN (1, 2) AND ((supplier_scope_mode_snapshot = 1 AND supplier_product_scope_id IS NULL AND supplier_product_scope_version_snapshot IS NULL AND supplier_scope_validation_result_snapshot = 'Unrestricted') OR (supplier_scope_mode_snapshot = 2 AND supplier_product_scope_id IS NOT NULL AND supplier_product_scope_version_snapshot > 0 AND supplier_scope_validation_result_snapshot = 'RestrictedScopeValidated')) AND bag_type_version_snapshot > 0 AND bag_construction_class_snapshot IN (1, 2, 3, 4) AND bag_tare_weight_kg_snapshot >= 0 AND ((product_standard_bag_weight_id IS NULL AND product_standard_bag_weight_version_snapshot IS NULL AND standard_bag_weight_label_snapshot IS NULL AND standard_content_weight_kg_snapshot IS NULL) OR (product_standard_bag_weight_id IS NOT NULL AND product_standard_bag_weight_version_snapshot > 0 AND standard_content_weight_kg_snapshot > 0)) AND processing_method_snapshot IN (0, 1, 2) AND char_length(weight_source) BETWEEN 1 AND 32");
                    table.ForeignKey(
                        name: "fk_commercial_receiving_entries_session",
                        columns: x => new { x.workspace_id, x.company_id, x.receiving_session_id },
                        principalSchema: "procurement",
                        principalTable: "commercial_receiving_sessions",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_receiving_ownerships",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiving_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    editor_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ownership_generation = table.Column<long>(type: "bigint", nullable: false),
                    lease_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_reacquired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_transferred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_transferred_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commercial_receiving_ownerships", x => x.id);
                    table.CheckConstraint("ck_commercial_receiving_ownerships_generation", "ownership_generation > 0 AND version > 0");
                    table.CheckConstraint("ck_commercial_receiving_ownerships_lease_shape", "(lease_id IS NULL AND lease_expires_at_utc IS NULL AND last_heartbeat_at_utc IS NULL) OR (lease_id IS NOT NULL AND lease_expires_at_utc IS NOT NULL AND last_heartbeat_at_utc IS NOT NULL AND lease_expires_at_utc > last_heartbeat_at_utc)");
                    table.ForeignKey(
                        name: "fk_commercial_receiving_ownerships_editor_device",
                        columns: x => new { x.workspace_id, x.editor_device_id },
                        principalSchema: "platform",
                        principalTable: "devices",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commercial_receiving_ownerships_session",
                        columns: x => new { x.workspace_id, x.company_id, x.receiving_session_id },
                        principalSchema: "procurement",
                        principalTable: "commercial_receiving_sessions",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_commercial_outbox_audiences_cursor",
                schema: "platform",
                table: "commercial_outbox_audiences",
                columns: new[] { "workspace_id", "company_id", "audience", "target_device_id", "outbox_message_id" });

            migrationBuilder.CreateIndex(
                name: "ix_commercial_outbox_audiences_workspace_target_device",
                schema: "platform",
                table: "commercial_outbox_audiences",
                columns: new[] { "workspace_id", "target_device_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_outbox_messages_event_stream",
                schema: "platform",
                table: "outbox_messages",
                sql: "event_stream IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "ix_commercial_master_changes_company_sequence",
                schema: "sync",
                table: "commercial_master_changes",
                columns: new[] { "workspace_id", "company_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ux_commercial_master_changes_version",
                schema: "sync",
                table: "commercial_master_changes",
                columns: new[] { "workspace_id", "company_id", "master_type", "master_id", "master_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_receiving_entries_company_operation",
                schema: "procurement",
                table: "commercial_receiving_entries",
                columns: new[] { "workspace_id", "company_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_receiving_entries_session_sequence",
                schema: "procurement",
                table: "commercial_receiving_entries",
                columns: new[] { "workspace_id", "company_id", "receiving_session_id", "local_sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commercial_receiving_ownerships_editor",
                schema: "procurement",
                table: "commercial_receiving_ownerships",
                columns: new[] { "workspace_id", "company_id", "editor_device_id", "receiving_session_id" });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_receiving_ownerships_workspace_id_editor_device_~",
                schema: "procurement",
                table: "commercial_receiving_ownerships",
                columns: new[] { "workspace_id", "editor_device_id" });

            migrationBuilder.CreateIndex(
                name: "ux_commercial_receiving_ownerships_session",
                schema: "procurement",
                table: "commercial_receiving_ownerships",
                columns: new[] { "workspace_id", "company_id", "receiving_session_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_receiving_reference_policies_company_document",
                schema: "procurement",
                table: "commercial_receiving_reference_policies",
                columns: new[] { "workspace_id", "company_id", "document_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commercial_receiving_sessions_company_list",
                schema: "procurement",
                table: "commercial_receiving_sessions",
                columns: new[] { "workspace_id", "company_id", "status", "updated_at_utc", "cloud_reference" });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_receiving_sessions_workspace_id_company_id_branc~",
                schema: "procurement",
                table: "commercial_receiving_sessions",
                columns: new[] { "workspace_id", "company_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "ux_commercial_receiving_sessions_company_reference",
                schema: "procurement",
                table: "commercial_receiving_sessions",
                columns: new[] { "workspace_id", "company_id", "cloud_reference" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE procurement.commercial_receiving_sessions
                    ADD CONSTRAINT ck_commercial_receiving_sessions_uuidv7
                    CHECK (uuid_extract_version(id) = 7);
                ALTER TABLE procurement.commercial_receiving_entries
                    ADD CONSTRAINT ck_commercial_receiving_entries_uuidv7
                    CHECK (uuid_extract_version(id) = 7 AND uuid_extract_version(operation_id) = 7),
                    ADD CONSTRAINT ck_commercial_receiving_entries_raw_positive
                    CHECK (raw_weight_kg ~ '^[0-9]{1,14}(\.[0-9]{1,6})?$' AND raw_weight_kg::numeric > 0);
                ALTER TABLE procurement.commercial_receiving_ownerships
                    ADD CONSTRAINT ck_commercial_receiving_ownerships_uuidv7
                    CHECK (uuid_extract_version(id) = 7 AND (lease_id IS NULL OR uuid_extract_version(lease_id) = 7));
                INSERT INTO procurement.commercial_receiving_reference_policies(
                    id, workspace_id, company_id, document_type, format_template,
                    reset_policy, starting_number, created_at_utc, updated_at_utc, version)
                SELECT id, workspace_id, id, 'CommercialReceiving', 'RCV-{SEQ:000000}',
                       1, 1, created_at_utc, created_at_utc, 1
                FROM platform.companies
                ON CONFLICT (workspace_id, company_id, document_type) DO NOTHING;

                CREATE FUNCTION procurement.create_default_commercial_receiving_reference_policy()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                BEGIN
                    INSERT INTO procurement.commercial_receiving_reference_policies(
                        id, workspace_id, company_id, document_type, format_template,
                        reset_policy, starting_number, created_at_utc, updated_at_utc, version)
                    VALUES (NEW.id, NEW.workspace_id, NEW.id, 'CommercialReceiving',
                            'RCV-{SEQ:000000}', 1, 1, NEW.created_at_utc,
                            NEW.created_at_utc, 1)
                    ON CONFLICT (workspace_id, company_id, document_type) DO NOTHING;
                    RETURN NEW;
                END;
                $function$;
                CREATE TRIGGER tr_companies_default_commercial_receiving_reference_policy
                AFTER INSERT ON platform.companies FOR EACH ROW
                EXECUTE FUNCTION procurement.create_default_commercial_receiving_reference_policy();

                CREATE FUNCTION procurement.reject_commercial_receiving_entry_mutation()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                BEGIN
                    RAISE EXCEPTION 'Commercial Receiving Entries are immutable.' USING ERRCODE = '55000';
                END;
                $function$;
                CREATE TRIGGER tr_commercial_receiving_entries_immutable
                BEFORE UPDATE OR DELETE ON procurement.commercial_receiving_entries
                FOR EACH ROW EXECUTE FUNCTION procurement.reject_commercial_receiving_entry_mutation();

                CREATE FUNCTION procurement.validate_commercial_receiving_aggregate()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                DECLARE
                    session_key uuid;
                    session_row procurement.commercial_receiving_sessions%ROWTYPE;
                    accepted_count bigint;
                    accepted_total numeric(20,6);
                    maximum_sequence bigint;
                    required_next bigint;
                BEGIN
                    session_key := CASE
                        WHEN TG_TABLE_NAME = 'commercial_receiving_entries' THEN (to_jsonb(NEW) ->> 'receiving_session_id')::uuid
                        ELSE (to_jsonb(NEW) ->> 'id')::uuid
                    END;
                    SELECT * INTO session_row
                    FROM procurement.commercial_receiving_sessions
                    WHERE id = session_key;
                    IF NOT FOUND THEN
                        RETURN NULL;
                    END IF;
                    SELECT count(*), COALESCE(sum(processed_weight_kg), 0), max(local_sequence)
                    INTO accepted_count, accepted_total, maximum_sequence
                    FROM procurement.commercial_receiving_entries
                    WHERE workspace_id = session_row.workspace_id
                      AND company_id = session_row.company_id
                      AND receiving_session_id = session_row.id;
                    required_next := 2 + accepted_count + CASE WHEN session_row.status = 2 THEN 1 ELSE 0 END;
                    IF session_row.entry_count <> accepted_count OR
                       session_row.processed_total_weight_kg <> accepted_total OR
                       session_row.next_expected_local_sequence <> required_next OR
                       maximum_sequence IS DISTINCT FROM (CASE WHEN accepted_count = 0 THEN NULL ELSE accepted_count + 1 END)
                    THEN
                        RAISE EXCEPTION 'Commercial Receiving aggregate sequence, count, and total must reconcile with immutable Entries.' USING ERRCODE = '55000';
                    END IF;
                    RETURN NULL;
                END;
                $function$;
                CREATE CONSTRAINT TRIGGER tr_commercial_receiving_sessions_reconcile
                AFTER INSERT OR UPDATE ON procurement.commercial_receiving_sessions
                DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
                EXECUTE FUNCTION procurement.validate_commercial_receiving_aggregate();
                CREATE CONSTRAINT TRIGGER tr_commercial_receiving_entries_reconcile
                AFTER INSERT ON procurement.commercial_receiving_entries
                DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
                EXECUTE FUNCTION procurement.validate_commercial_receiving_aggregate();

                CREATE FUNCTION procurement.validate_commercial_receiving_ownership_state()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                DECLARE
                    session_status smallint;
                BEGIN
                    SELECT status INTO session_status
                    FROM procurement.commercial_receiving_sessions
                    WHERE workspace_id = NEW.workspace_id
                      AND company_id = NEW.company_id
                      AND id = NEW.receiving_session_id;
                    IF NEW.lease_id IS NULL THEN
                        IF session_status <> 2 THEN
                            RAISE EXCEPTION 'Only a submitted Commercial Receiving Session may have closed ownership.' USING ERRCODE = '55000';
                        END IF;
                    ELSE
                        IF session_status <> 1 OR NOT EXISTS (
                            SELECT 1
                            FROM platform.devices d
                            JOIN platform.device_credentials c
                              ON c.workspace_id = d.workspace_id
                             AND c.device_id = d.id
                             AND c.revoked_at_utc IS NULL
                            WHERE d.workspace_id = NEW.workspace_id
                              AND d.id = NEW.editor_device_id
                              AND d.status = 1)
                        THEN
                            RAISE EXCEPTION 'Active Commercial Receiving ownership requires an in-progress Session and active credentialed Device.' USING ERRCODE = '55000';
                        END IF;
                    END IF;
                    RETURN NULL;
                END;
                $function$;
                CREATE CONSTRAINT TRIGGER tr_commercial_receiving_ownerships_validate
                AFTER INSERT OR UPDATE ON procurement.commercial_receiving_ownerships
                DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
                EXECUTE FUNCTION procurement.validate_commercial_receiving_ownership_state();

                CREATE FUNCTION procurement.guard_commercial_receiving_session_mutation()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Commercial Receiving Sessions cannot be deleted.' USING ERRCODE = '55000';
                    END IF;
                    IF ROW(NEW.id, NEW.workspace_id, NEW.company_id, NEW.branch_id,
                           NEW.cloud_reference, NEW.cloud_reference_sequence,
                           NEW.reference_policy_version_snapshot, NEW.external_reference,
                           NEW.supplier_id, NEW.supplier_version_snapshot,
                           NEW.supplier_code_snapshot, NEW.supplier_name_snapshot,
                           NEW.supplier_product_scope_mode_snapshot,
                           NEW.company_procurement_settings_id,
                           NEW.procurement_settings_version_snapshot,
                           NEW.vehicle_selection_mode_snapshot,
                           NEW.destination_location_id,
                           NEW.destination_location_version_snapshot,
                           NEW.destination_location_code_snapshot,
                           NEW.destination_location_name_snapshot,
                           NEW.weight_processing_policy_id,
                           NEW.weight_policy_version_snapshot,
                           NEW.weight_decimal_places_snapshot,
                           NEW.weight_processing_method_snapshot,
                           NEW.receiving_vehicle_id,
                           NEW.receiving_vehicle_version_snapshot,
                           NEW.vehicle_code_snapshot, NEW.vehicle_registration_snapshot,
                           NEW.vehicle_display_name_snapshot,
                           NEW.started_at_device_utc, NEW.started_at_server_utc,
                           NEW.created_at_utc)
                       IS DISTINCT FROM
                       ROW(OLD.id, OLD.workspace_id, OLD.company_id, OLD.branch_id,
                           OLD.cloud_reference, OLD.cloud_reference_sequence,
                           OLD.reference_policy_version_snapshot, OLD.external_reference,
                           OLD.supplier_id, OLD.supplier_version_snapshot,
                           OLD.supplier_code_snapshot, OLD.supplier_name_snapshot,
                           OLD.supplier_product_scope_mode_snapshot,
                           OLD.company_procurement_settings_id,
                           OLD.procurement_settings_version_snapshot,
                           OLD.vehicle_selection_mode_snapshot,
                           OLD.destination_location_id,
                           OLD.destination_location_version_snapshot,
                           OLD.destination_location_code_snapshot,
                           OLD.destination_location_name_snapshot,
                           OLD.weight_processing_policy_id,
                           OLD.weight_policy_version_snapshot,
                           OLD.weight_decimal_places_snapshot,
                           OLD.weight_processing_method_snapshot,
                           OLD.receiving_vehicle_id,
                           OLD.receiving_vehicle_version_snapshot,
                           OLD.vehicle_code_snapshot, OLD.vehicle_registration_snapshot,
                           OLD.vehicle_display_name_snapshot,
                           OLD.started_at_device_utc, OLD.started_at_server_utc,
                           OLD.created_at_utc)
                    THEN
                        RAISE EXCEPTION 'Commercial Receiving identity and snapshots are immutable.' USING ERRCODE = '55000';
                    END IF;
                    IF NEW.version <> OLD.version + 1 OR NEW.updated_at_utc < OLD.updated_at_utc THEN
                        RAISE EXCEPTION 'Commercial Receiving Session revisions advance exactly once.' USING ERRCODE = '55000';
                    END IF;
                    IF OLD.status = 2 OR (OLD.status = 1 AND NEW.status NOT IN (1, 2)) THEN
                        RAISE EXCEPTION 'Commercial Receiving status transition is invalid.' USING ERRCODE = '55000';
                    END IF;
                    IF OLD.status = 1 AND NEW.status = 1 AND NOT (
                        (NEW.next_expected_local_sequence = OLD.next_expected_local_sequence + 1 AND
                         NEW.entry_count = OLD.entry_count + 1 AND
                         NEW.processed_total_weight_kg > OLD.processed_total_weight_kg AND
                         NEW.submitted_at_utc IS NULL) OR
                        (NEW.next_expected_local_sequence = OLD.next_expected_local_sequence AND
                         NEW.entry_count = OLD.entry_count AND
                         NEW.processed_total_weight_kg = OLD.processed_total_weight_kg AND
                         NEW.submitted_at_utc IS NULL))
                    THEN
                        RAISE EXCEPTION 'Commercial Receiving in-progress aggregate transition is invalid.' USING ERRCODE = '55000';
                    END IF;
                    IF OLD.status = 1 AND NEW.status = 2 AND NOT (
                        NEW.next_expected_local_sequence = OLD.next_expected_local_sequence + 1 AND
                        NEW.entry_count = OLD.entry_count AND
                        NEW.processed_total_weight_kg = OLD.processed_total_weight_kg AND
                        NEW.submitted_at_utc IS NOT NULL)
                    THEN
                        RAISE EXCEPTION 'Commercial Receiving submission transition is invalid.' USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $function$;
                CREATE TRIGGER tr_commercial_receiving_sessions_guard
                BEFORE UPDATE OR DELETE ON procurement.commercial_receiving_sessions
                FOR EACH ROW EXECUTE FUNCTION procurement.guard_commercial_receiving_session_mutation();

                CREATE FUNCTION procurement.guard_commercial_receiving_ownership_mutation()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Commercial Receiving ownership cannot be deleted.' USING ERRCODE = '55000';
                    END IF;
                    IF ROW(NEW.id, NEW.workspace_id, NEW.company_id, NEW.receiving_session_id, NEW.created_at_utc)
                       IS DISTINCT FROM ROW(OLD.id, OLD.workspace_id, OLD.company_id, OLD.receiving_session_id, OLD.created_at_utc)
                    THEN
                        RAISE EXCEPTION 'Commercial Receiving ownership identity is immutable.' USING ERRCODE = '55000';
                    END IF;
                    IF NEW.version <> OLD.version + 1 OR NEW.updated_at_utc < OLD.updated_at_utc OR
                       NEW.ownership_generation NOT IN (OLD.ownership_generation, OLD.ownership_generation + 1) OR
                       ((NEW.editor_device_id IS DISTINCT FROM OLD.editor_device_id) <> (NEW.ownership_generation = OLD.ownership_generation + 1))
                    THEN
                        RAISE EXCEPTION 'Commercial Receiving ownership transition is invalid.' USING ERRCODE = '55000';
                    END IF;
                    IF NEW.ownership_generation = OLD.ownership_generation AND (
                       ROW(NEW.last_transferred_at_utc, NEW.last_transferred_by_user_id)
                           IS DISTINCT FROM ROW(OLD.last_transferred_at_utc, OLD.last_transferred_by_user_id) OR
                       ((NEW.last_reacquired_at_utc IS DISTINCT FROM OLD.last_reacquired_at_utc) <>
                        (NEW.lease_id IS DISTINCT FROM OLD.lease_id AND NEW.lease_id IS NOT NULL)))
                    THEN
                        RAISE EXCEPTION 'Commercial Receiving renewal or reacquisition transition is invalid.' USING ERRCODE = '55000';
                    END IF;
                    IF NEW.ownership_generation = OLD.ownership_generation + 1 AND (
                       NEW.lease_id IS NULL OR NEW.lease_id IS NOT DISTINCT FROM OLD.lease_id OR
                       NEW.last_transferred_at_utc IS NULL OR NEW.last_transferred_at_utc IS NOT DISTINCT FROM OLD.last_transferred_at_utc OR
                       NEW.last_transferred_by_user_id IS NULL OR
                       NEW.last_reacquired_at_utc IS DISTINCT FROM OLD.last_reacquired_at_utc)
                    THEN
                        RAISE EXCEPTION 'Commercial Receiving transfer transition is invalid.' USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END;
                $function$;
                CREATE TRIGGER tr_commercial_receiving_ownerships_guard
                BEFORE UPDATE OR DELETE ON procurement.commercial_receiving_ownerships
                FOR EACH ROW EXECUTE FUNCTION procurement.guard_commercial_receiving_ownership_mutation();

                CREATE FUNCTION procurement.guard_commercial_receiving_reference_fact()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Commercial Receiving reference facts cannot be deleted.' USING ERRCODE = '55000';
                    END IF;
                    IF TG_TABLE_NAME = 'commercial_receiving_reference_counters' THEN
                        IF ROW(NEW.workspace_id, NEW.company_id, NEW.policy_id, NEW.period_key)
                           IS DISTINCT FROM ROW(OLD.workspace_id, OLD.company_id, OLD.policy_id, OLD.period_key) OR
                           NEW.next_number <= OLD.next_number OR NEW.version <> OLD.version + 1 THEN
                            RAISE EXCEPTION 'Commercial Receiving reference counter transition is invalid.' USING ERRCODE = '55000';
                        END IF;
                    ELSE
                        IF ROW(NEW.id, NEW.workspace_id, NEW.company_id, NEW.document_type, NEW.created_at_utc)
                           IS DISTINCT FROM ROW(OLD.id, OLD.workspace_id, OLD.company_id, OLD.document_type, OLD.created_at_utc) OR
                           NEW.version <> OLD.version + 1 THEN
                            RAISE EXCEPTION 'Commercial Receiving reference policy transition is invalid.' USING ERRCODE = '55000';
                        END IF;
                        IF NEW.starting_number <> OLD.starting_number AND EXISTS (
                            SELECT 1 FROM procurement.commercial_receiving_reference_counters c
                            WHERE c.workspace_id = OLD.workspace_id AND c.company_id = OLD.company_id
                              AND c.policy_id = OLD.id AND c.next_number > OLD.starting_number) THEN
                            RAISE EXCEPTION 'Commercial Receiving reference series has started.' USING ERRCODE = '55000';
                        END IF;
                    END IF;
                    RETURN NEW;
                END;
                $function$;
                CREATE TRIGGER tr_commercial_receiving_reference_policies_guard
                BEFORE UPDATE OR DELETE ON procurement.commercial_receiving_reference_policies
                FOR EACH ROW EXECUTE FUNCTION procurement.guard_commercial_receiving_reference_fact();
                CREATE TRIGGER tr_commercial_receiving_reference_counters_guard
                BEFORE UPDATE OR DELETE ON procurement.commercial_receiving_reference_counters
                FOR EACH ROW EXECUTE FUNCTION procurement.guard_commercial_receiving_reference_fact();

                CREATE FUNCTION sync.reject_commercial_master_change_mutation()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                BEGIN
                    RAISE EXCEPTION 'Commercial master change facts are immutable.' USING ERRCODE = '55000';
                END;
                $function$;
                CREATE TRIGGER tr_commercial_master_changes_immutable
                BEFORE UPDATE OR DELETE ON sync.commercial_master_changes
                FOR EACH ROW EXECUTE FUNCTION sync.reject_commercial_master_change_mutation();

                CREATE FUNCTION platform.reject_commercial_outbox_audience_mutation()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                BEGIN
                    RAISE EXCEPTION 'Commercial outbox audience facts are immutable.' USING ERRCODE = '55000';
                END;
                $function$;
                CREATE TRIGGER tr_commercial_outbox_audiences_immutable
                BEFORE UPDATE OR DELETE ON platform.commercial_outbox_audiences
                FOR EACH ROW EXECUTE FUNCTION platform.reject_commercial_outbox_audience_mutation();
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION sync.capture_commercial_master_change()
                RETURNS trigger LANGUAGE plpgsql AS $function$
                DECLARE
                    source jsonb := to_jsonb(NEW);
                    kind text;
                    safe_payload jsonb;
                    status_text text;
                    workspace_value uuid := (source ->> 'workspace_id')::uuid;
                    company_value uuid := (source ->> 'company_id')::uuid;
                BEGIN
                    kind := CASE TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME
                        WHEN 'procurement.company_procurement_settings' THEN 'CompanyProcurementSettings'
                        WHEN 'operations.business_locations' THEN 'BusinessLocation'
                        WHEN 'procurement.receiving_vehicles' THEN 'ReceivingVehicle'
                        WHEN 'procurement.bag_types' THEN 'BagType'
                        WHEN 'procurement.weight_processing_policies' THEN 'WeightProcessingPolicy'
                        WHEN 'procurement.suppliers' THEN 'Supplier'
                        WHEN 'procurement.supplier_product_scopes' THEN 'SupplierProductScope'
                        WHEN 'catalog.product_groups' THEN 'ProductGroup'
                        WHEN 'catalog.products' THEN 'Product'
                        WHEN 'catalog.product_standard_bag_weights' THEN 'ProductStandardBagWeight'
                    END;
                    status_text := CASE COALESCE(source ->> 'status', '1') WHEN '1' THEN 'Active' ELSE 'Inactive' END;
                    safe_payload := CASE kind
                        WHEN 'BusinessLocation' THEN source - ARRAY['address_line','notes']
                        WHEN 'ReceivingVehicle' THEN source - ARRAY['owner_name','contact_number','notes']
                        WHEN 'BagType' THEN source - 'notes'
                        WHEN 'WeightProcessingPolicy' THEN source - 'notes'
                        WHEN 'Supplier' THEN source - ARRAY['contact_name','contact_number','email','address_line','tax_registration_number','normalized_tax_registration_number','notes']
                        WHEN 'Product' THEN source - 'notes'
                        ELSE source
                    END;
                    PERFORM pg_advisory_xact_lock(hashtextextended(
                        'TraderPro.CommercialMasterSync.CommitOrder.v1' || E'\n' || workspace_value::text || E'\n' || company_value::text, 0));
                    INSERT INTO sync.commercial_master_changes(
                        workspace_id, company_id, master_type, master_id,
                        master_version, status, payload_json, occurred_at_utc)
                    VALUES (workspace_value, company_value, kind,
                            (source ->> 'id')::uuid, (source ->> 'version')::bigint,
                            status_text, safe_payload,
                            (source ->> 'updated_at_utc')::timestamptz)
                    ON CONFLICT (workspace_id, company_id, master_type, master_id, master_version)
                    DO NOTHING;
                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER tr_company_procurement_settings_commercial_sync AFTER INSERT OR UPDATE ON procurement.company_procurement_settings FOR EACH ROW EXECUTE FUNCTION sync.capture_commercial_master_change();
                CREATE TRIGGER tr_business_locations_commercial_sync AFTER INSERT OR UPDATE ON operations.business_locations FOR EACH ROW EXECUTE FUNCTION sync.capture_commercial_master_change();
                CREATE TRIGGER tr_receiving_vehicles_commercial_sync AFTER INSERT OR UPDATE ON procurement.receiving_vehicles FOR EACH ROW EXECUTE FUNCTION sync.capture_commercial_master_change();
                CREATE TRIGGER tr_bag_types_commercial_sync AFTER INSERT OR UPDATE ON procurement.bag_types FOR EACH ROW EXECUTE FUNCTION sync.capture_commercial_master_change();
                CREATE TRIGGER tr_weight_processing_policies_commercial_sync AFTER INSERT OR UPDATE ON procurement.weight_processing_policies FOR EACH ROW EXECUTE FUNCTION sync.capture_commercial_master_change();
                CREATE TRIGGER tr_suppliers_commercial_sync AFTER INSERT OR UPDATE ON procurement.suppliers FOR EACH ROW EXECUTE FUNCTION sync.capture_commercial_master_change();
                CREATE TRIGGER tr_supplier_product_scopes_commercial_sync AFTER INSERT OR UPDATE ON procurement.supplier_product_scopes FOR EACH ROW EXECUTE FUNCTION sync.capture_commercial_master_change();
                CREATE TRIGGER tr_product_groups_commercial_sync AFTER INSERT OR UPDATE ON catalog.product_groups FOR EACH ROW EXECUTE FUNCTION sync.capture_commercial_master_change();
                CREATE TRIGGER tr_products_commercial_sync AFTER INSERT OR UPDATE ON catalog.products FOR EACH ROW EXECUTE FUNCTION sync.capture_commercial_master_change();
                CREATE TRIGGER tr_product_standard_bag_weights_commercial_sync AFTER INSERT OR UPDATE ON catalog.product_standard_bag_weights FOR EACH ROW EXECUTE FUNCTION sync.capture_commercial_master_change();

                INSERT INTO sync.commercial_master_changes(
                    workspace_id, company_id, master_type, master_id,
                    master_version, status, payload_json, occurred_at_utc)
                SELECT workspace_id, company_id, master_type, master_id,
                       master_version, status, payload_json, occurred_at_utc
                FROM (
                    SELECT workspace_id, company_id, 'CompanyProcurementSettings'::text master_type, id master_id, version master_version, 'Active'::text status, to_jsonb(t) payload_json, updated_at_utc occurred_at_utc FROM procurement.company_procurement_settings t
                    UNION ALL SELECT workspace_id, company_id, 'BusinessLocation', id, version, CASE status WHEN 1 THEN 'Active' ELSE 'Inactive' END, to_jsonb(t) - ARRAY['address_line','notes'], updated_at_utc FROM operations.business_locations t
                    UNION ALL SELECT workspace_id, company_id, 'ReceivingVehicle', id, version, CASE status WHEN 1 THEN 'Active' ELSE 'Inactive' END, to_jsonb(t) - ARRAY['owner_name','contact_number','notes'], updated_at_utc FROM procurement.receiving_vehicles t
                    UNION ALL SELECT workspace_id, company_id, 'BagType', id, version, CASE status WHEN 1 THEN 'Active' ELSE 'Inactive' END, to_jsonb(t) - 'notes', updated_at_utc FROM procurement.bag_types t
                    UNION ALL SELECT workspace_id, company_id, 'WeightProcessingPolicy', id, version, CASE status WHEN 1 THEN 'Active' ELSE 'Inactive' END, to_jsonb(t) - 'notes', updated_at_utc FROM procurement.weight_processing_policies t
                    UNION ALL SELECT workspace_id, company_id, 'Supplier', id, version, CASE status WHEN 1 THEN 'Active' ELSE 'Inactive' END, to_jsonb(t) - ARRAY['contact_name','contact_number','email','address_line','tax_registration_number','normalized_tax_registration_number','notes'], updated_at_utc FROM procurement.suppliers t
                    UNION ALL SELECT workspace_id, company_id, 'SupplierProductScope', id, version, CASE status WHEN 1 THEN 'Active' ELSE 'Inactive' END, to_jsonb(t), updated_at_utc FROM procurement.supplier_product_scopes t
                    UNION ALL SELECT workspace_id, company_id, 'ProductGroup', id, version, CASE status WHEN 1 THEN 'Active' ELSE 'Inactive' END, to_jsonb(t), updated_at_utc FROM catalog.product_groups t
                    UNION ALL SELECT workspace_id, company_id, 'Product', id, version, CASE status WHEN 1 THEN 'Active' ELSE 'Inactive' END, to_jsonb(t) - 'notes', updated_at_utc FROM catalog.products t
                    UNION ALL SELECT workspace_id, company_id, 'ProductStandardBagWeight', id, version, CASE status WHEN 1 THEN 'Active' ELSE 'Inactive' END, to_jsonb(t), updated_at_utc FROM catalog.product_standard_bag_weights t
                ) backfill
                ORDER BY master_type, master_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS tr_company_procurement_settings_commercial_sync ON procurement.company_procurement_settings;
                DROP TRIGGER IF EXISTS tr_business_locations_commercial_sync ON operations.business_locations;
                DROP TRIGGER IF EXISTS tr_receiving_vehicles_commercial_sync ON procurement.receiving_vehicles;
                DROP TRIGGER IF EXISTS tr_bag_types_commercial_sync ON procurement.bag_types;
                DROP TRIGGER IF EXISTS tr_weight_processing_policies_commercial_sync ON procurement.weight_processing_policies;
                DROP TRIGGER IF EXISTS tr_suppliers_commercial_sync ON procurement.suppliers;
                DROP TRIGGER IF EXISTS tr_supplier_product_scopes_commercial_sync ON procurement.supplier_product_scopes;
                DROP TRIGGER IF EXISTS tr_product_groups_commercial_sync ON catalog.product_groups;
                DROP TRIGGER IF EXISTS tr_products_commercial_sync ON catalog.products;
                DROP TRIGGER IF EXISTS tr_product_standard_bag_weights_commercial_sync ON catalog.product_standard_bag_weights;
                DROP FUNCTION IF EXISTS sync.capture_commercial_master_change();
                DROP TRIGGER IF EXISTS tr_companies_default_commercial_receiving_reference_policy ON platform.companies;
                DROP FUNCTION IF EXISTS procurement.create_default_commercial_receiving_reference_policy();
                DROP FUNCTION IF EXISTS procurement.reject_commercial_receiving_entry_mutation() CASCADE;
                DROP FUNCTION IF EXISTS procurement.validate_commercial_receiving_aggregate() CASCADE;
                DROP FUNCTION IF EXISTS procurement.validate_commercial_receiving_ownership_state() CASCADE;
                DROP FUNCTION IF EXISTS procurement.guard_commercial_receiving_session_mutation() CASCADE;
                DROP FUNCTION IF EXISTS procurement.guard_commercial_receiving_ownership_mutation() CASCADE;
                DROP FUNCTION IF EXISTS procurement.guard_commercial_receiving_reference_fact() CASCADE;
                DROP FUNCTION IF EXISTS sync.reject_commercial_master_change_mutation() CASCADE;
                DROP FUNCTION IF EXISTS platform.reject_commercial_outbox_audience_mutation() CASCADE;
                """);
            migrationBuilder.DropTable(
                name: "commercial_outbox_audiences",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "commercial_master_changes",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "commercial_receiving_entries",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "commercial_receiving_ownerships",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "commercial_receiving_reference_counters",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "commercial_receiving_sessions",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "commercial_receiving_reference_policies",
                schema: "procurement");

            migrationBuilder.DropCheckConstraint(
                name: "ck_outbox_messages_event_stream",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.AddCheckConstraint(
                name: "ck_outbox_messages_event_stream",
                schema: "platform",
                table: "outbox_messages",
                sql: "event_stream IN (1, 2)");
        }
    }
}
