using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TraderProDbContext))]
[Migration("20260802090000_HardenCommercialReceivingBackendContracts")]
public partial class HardenCommercialReceivingBackendContracts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_commercial_receiving_sessions_workspace_id_company_id_branc~",
            schema: "procurement",
            table: "commercial_receiving_sessions");

        migrationBuilder.AddUniqueConstraint(
            name: "ak_company_procurement_settings_workspace_company_id",
            schema: "procurement",
            table: "company_procurement_settings",
            columns: new[] { "workspace_id", "company_id", "id" });

        migrationBuilder.CreateTable(
            name: "commercial_receiving_operation_claims",
            schema: "sync",
            columns: table => new
            {
                workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                company_id = table.Column<Guid>(type: "uuid", nullable: false),
                command_scope = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                operation_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                session_id = table.Column<Guid>(type: "uuid", nullable: false),
                device_id = table.Column<Guid>(type: "uuid", nullable: false),
                ownership_generation = table.Column<long>(type: "bigint", nullable: true),
                request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                state = table.Column<short>(type: "smallint", nullable: false),
                attention_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                attention_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                retryable = table.Column<bool>(type: "boolean", nullable: false),
                first_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_commercial_receiving_operation_claims", x => new { x.workspace_id, x.company_id, x.command_scope, x.operation_id });
                table.UniqueConstraint("ak_commercial_receiving_operation_claims_company_operation", x => new { x.workspace_id, x.company_id, x.operation_id });
                table.CheckConstraint("ck_commercial_receiving_operation_claims_scope", "command_scope = 'Procurement.CommercialReceiving.MobileSyncOperation'");
                table.CheckConstraint("ck_commercial_receiving_operation_claims_state", "state IN (1, 2, 3, 4)");
                table.CheckConstraint("ck_commercial_receiving_operation_claims_shape", "char_length(request_hash) = 64 AND request_hash ~ '^[0-9a-f]{64}$' AND (ownership_generation IS NULL OR ownership_generation > 0) AND ((state IN (1, 4) AND attention_code IS NULL AND attention_message IS NULL) OR (state IN (2, 3) AND attention_code IS NOT NULL AND attention_message IS NOT NULL)) AND ((state = 4 AND completed_at_utc IS NOT NULL) OR (state <> 4 AND completed_at_utc IS NULL))");
                table.ForeignKey(
                    name: "fk_commercial_receiving_operation_claims_company",
                    columns: x => new { x.workspace_id, x.company_id },
                    principalSchema: "platform",
                    principalTable: "companies",
                    principalColumns: new[] { "workspace_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_commercial_receiving_operation_claims_device",
                    columns: x => new { x.workspace_id, x.device_id },
                    principalSchema: "platform",
                    principalTable: "devices",
                    principalColumns: new[] { "workspace_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "commercial_receiving_reference_reservations",
            schema: "procurement",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                company_id = table.Column<Guid>(type: "uuid", nullable: false),
                policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                period_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                session_id = table.Column<Guid>(type: "uuid", nullable: false),
                request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                policy_version = table.Column<long>(type: "bigint", nullable: false),
                sequence = table.Column<long>(type: "bigint", nullable: false),
                rendered_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                reserved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_commercial_receiving_reference_reservations", x => x.id);
                table.UniqueConstraint("ak_commercial_receiving_reference_reservations_scope_id", x => new { x.workspace_id, x.company_id, x.id });
                table.CheckConstraint("ck_commercial_receiving_reference_reservations_shape", "sequence > 0 AND policy_version > 0 AND char_length(request_hash) = 64 AND request_hash ~ '^[0-9a-f]{64}$'");
                table.ForeignKey(
                    name: "fk_commercial_receiving_reference_reservations_claim",
                    columns: x => new { x.workspace_id, x.company_id, x.operation_id },
                    principalSchema: "sync",
                    principalTable: "commercial_receiving_operation_claims",
                    principalColumns: new[] { "workspace_id", "company_id", "operation_id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_commercial_receiving_reference_reservations_policy",
                    columns: x => new { x.workspace_id, x.company_id, x.policy_id },
                    principalSchema: "procurement",
                    principalTable: "commercial_receiving_reference_policies",
                    principalColumns: new[] { "workspace_id", "company_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<Guid>(
            name: "reference_reservation_id",
            schema: "procurement",
            table: "commercial_receiving_sessions",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<long>(
            name: "ownership_generation",
            schema: "procurement",
            table: "commercial_receiving_sessions",
            type: "bigint",
            nullable: false,
            defaultValue: 1L);

        migrationBuilder.CreateIndex(
            name: "ix_commercial_receiving_operation_claims_attention",
            schema: "sync",
            table: "commercial_receiving_operation_claims",
            columns: new[] { "workspace_id", "company_id", "state", "updated_at_utc" });
        migrationBuilder.CreateIndex(
            name: "ix_commercial_receiving_operation_claims_workspace_device",
            schema: "sync",
            table: "commercial_receiving_operation_claims",
            columns: new[] { "workspace_id", "device_id" });
        migrationBuilder.CreateIndex(
            name: "ux_commercial_receiving_reference_reservations_operation",
            schema: "procurement",
            table: "commercial_receiving_reference_reservations",
            columns: new[] { "workspace_id", "company_id", "operation_id" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "ux_commercial_receiving_reference_reservations_reference",
            schema: "procurement",
            table: "commercial_receiving_reference_reservations",
            columns: new[] { "workspace_id", "company_id", "rendered_reference" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "ux_commercial_receiving_reference_reservations_sequence",
            schema: "procurement",
            table: "commercial_receiving_reference_reservations",
            columns: new[] { "workspace_id", "company_id", "policy_id", "period_key", "sequence" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "ix_cr_sessions_destination",
            schema: "procurement",
            table: "commercial_receiving_sessions",
            columns: new[] { "workspace_id", "company_id", "branch_id", "destination_location_id" });
        migrationBuilder.CreateIndex(
            name: "ix_cr_entries_standard_weight",
            schema: "procurement",
            table: "commercial_receiving_entries",
            columns: new[] { "workspace_id", "company_id", "product_standard_bag_weight_id" });

        migrationBuilder.Sql(BackfillLegacyReceivingSql);

        migrationBuilder.AlterColumn<Guid>(
            name: "reference_reservation_id",
            schema: "procurement",
            table: "commercial_receiving_sessions",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        AddReceivingForeignKeys(migrationBuilder);
        AddFunctionBlocks(migrationBuilder, IntegrityFunctionsSql);
        AddFunctionBlocks(migrationBuilder, TransitionAndAudienceSql);
        AddFunctionBlocks(migrationBuilder, MasterPayloadFunctionsSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DropIntegrityFunctionsSql);
        migrationBuilder.Sql(RestoreLegacyMasterCaptureSql);
        DropReceivingForeignKeys(migrationBuilder);
        migrationBuilder.DropIndex(
            name: "ix_cr_sessions_destination",
            schema: "procurement",
            table: "commercial_receiving_sessions");
        migrationBuilder.DropIndex(
            name: "ix_cr_entries_standard_weight",
            schema: "procurement",
            table: "commercial_receiving_entries");
        migrationBuilder.DropColumn(name: "ownership_generation", schema: "procurement", table: "commercial_receiving_sessions");
        migrationBuilder.DropColumn(name: "reference_reservation_id", schema: "procurement", table: "commercial_receiving_sessions");
        migrationBuilder.DropTable(name: "commercial_receiving_reference_reservations", schema: "procurement");
        migrationBuilder.DropTable(name: "commercial_receiving_operation_claims", schema: "sync");
        migrationBuilder.DropUniqueConstraint(
            name: "ak_company_procurement_settings_workspace_company_id",
            schema: "procurement",
            table: "company_procurement_settings");
        migrationBuilder.CreateIndex(
            name: "IX_commercial_receiving_sessions_workspace_id_company_id_branc~",
            schema: "procurement",
            table: "commercial_receiving_sessions",
            columns: new[] { "workspace_id", "company_id", "branch_id" });
    }

    private static void AddReceivingForeignKeys(MigrationBuilder migrationBuilder)
    {
        AddForeignKey(migrationBuilder, "fk_commercial_receiving_sessions_reference_reservation", "commercial_receiving_sessions", new[] { "workspace_id", "company_id", "reference_reservation_id" }, "procurement", "commercial_receiving_reference_reservations", new[] { "workspace_id", "company_id", "id" });
        AddForeignKey(migrationBuilder, "fk_commercial_receiving_sessions_supplier", "commercial_receiving_sessions", new[] { "workspace_id", "company_id", "supplier_id" }, "procurement", "suppliers", new[] { "workspace_id", "company_id", "id" });
        AddForeignKey(migrationBuilder, "fk_commercial_receiving_sessions_settings", "commercial_receiving_sessions", new[] { "workspace_id", "company_id", "company_procurement_settings_id" }, "procurement", "company_procurement_settings", new[] { "workspace_id", "company_id", "id" });
        AddForeignKey(migrationBuilder, "fk_commercial_receiving_sessions_destination", "commercial_receiving_sessions", new[] { "workspace_id", "company_id", "branch_id", "destination_location_id" }, "operations", "business_locations", new[] { "workspace_id", "company_id", "branch_id", "id" });
        AddForeignKey(migrationBuilder, "fk_commercial_receiving_sessions_weight_policy", "commercial_receiving_sessions", new[] { "workspace_id", "company_id", "weight_processing_policy_id" }, "procurement", "weight_processing_policies", new[] { "workspace_id", "company_id", "id" });
        AddForeignKey(migrationBuilder, "fk_commercial_receiving_sessions_vehicle", "commercial_receiving_sessions", new[] { "workspace_id", "company_id", "receiving_vehicle_id" }, "procurement", "receiving_vehicles", new[] { "workspace_id", "company_id", "id" });
        AddForeignKey(migrationBuilder, "fk_commercial_receiving_entries_product", "commercial_receiving_entries", new[] { "workspace_id", "company_id", "product_id" }, "catalog", "products", new[] { "workspace_id", "company_id", "id" });
        AddForeignKey(migrationBuilder, "fk_commercial_receiving_entries_supplier_scope", "commercial_receiving_entries", new[] { "workspace_id", "company_id", "supplier_product_scope_id" }, "procurement", "supplier_product_scopes", new[] { "workspace_id", "company_id", "id" });
        AddForeignKey(migrationBuilder, "fk_commercial_receiving_entries_bag_type", "commercial_receiving_entries", new[] { "workspace_id", "company_id", "bag_type_id" }, "procurement", "bag_types", new[] { "workspace_id", "company_id", "id" });
        AddForeignKey(migrationBuilder, "fk_commercial_receiving_entries_standard_weight", "commercial_receiving_entries", new[] { "workspace_id", "company_id", "product_standard_bag_weight_id" }, "catalog", "product_standard_bag_weights", new[] { "workspace_id", "company_id", "id" });
    }

    private static void AddForeignKey(
        MigrationBuilder migrationBuilder,
        string name,
        string table,
        string[] columns,
        string principalSchema,
        string principalTable,
        string[] principalColumns) =>
        migrationBuilder.AddForeignKey(
            name: name,
            schema: "procurement",
            table: table,
            columns: columns,
            principalSchema: principalSchema,
            principalTable: principalTable,
            principalColumns: principalColumns,
            onDelete: ReferentialAction.Restrict);

    private static void AddFunctionBlocks(MigrationBuilder migrationBuilder, string sql)
    {
        var blocks = sql.Split("\nCREATE FUNCTION ", StringSplitOptions.RemoveEmptyEntries);
        migrationBuilder.Sql(blocks[0]);
        foreach (var block in blocks.Skip(1))
        {
            migrationBuilder.Sql("CREATE FUNCTION " + block);
        }
    }

    private static void DropReceivingForeignKeys(MigrationBuilder migrationBuilder)
    {
        foreach (var (table, name) in new[]
        {
            ("commercial_receiving_sessions", "fk_commercial_receiving_sessions_reference_reservation"),
            ("commercial_receiving_sessions", "fk_commercial_receiving_sessions_supplier"),
            ("commercial_receiving_sessions", "fk_commercial_receiving_sessions_settings"),
            ("commercial_receiving_sessions", "fk_commercial_receiving_sessions_destination"),
            ("commercial_receiving_sessions", "fk_commercial_receiving_sessions_weight_policy"),
            ("commercial_receiving_sessions", "fk_commercial_receiving_sessions_vehicle"),
            ("commercial_receiving_entries", "fk_commercial_receiving_entries_product"),
            ("commercial_receiving_entries", "fk_commercial_receiving_entries_supplier_scope"),
            ("commercial_receiving_entries", "fk_commercial_receiving_entries_bag_type"),
            ("commercial_receiving_entries", "fk_commercial_receiving_entries_standard_weight"),
        })
        {
            migrationBuilder.DropForeignKey(name, "procurement", table);
        }
    }
}
