using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialOperationalMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operations");

            migrationBuilder.CreateTable(
                name: "bag_types",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    normalized_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    local_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    construction_class = table.Column<short>(type: "smallint", nullable: false),
                    standard_tare_weight_kg = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    is_returnable = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bag_types", x => x.id);
                    table.UniqueConstraint("ak_bag_types_workspace_company_id", x => new { x.workspace_id, x.company_id, x.id });
                    table.CheckConstraint("ck_bag_types_code", "code = normalized_code\nAND normalized_code ~ '^[A-Z0-9-]{2,32}$'");
                    table.CheckConstraint("ck_bag_types_construction_class", "construction_class IN (1, 2, 3, 4)");
                    table.CheckConstraint("ck_bag_types_status", "status IN (1, 2)");
                    table.CheckConstraint("ck_bag_types_tare_weight", "standard_tare_weight_kg >= 0");
                    table.CheckConstraint("ck_bag_types_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_bag_types_company",
                        columns: x => new { x.workspace_id, x.company_id },
                        principalSchema: "platform",
                        principalTable: "companies",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "business_locations",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    normalized_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    local_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    location_type = table.Column<short>(type: "smallint", nullable: false),
                    address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_business_locations", x => x.id);
                    table.UniqueConstraint("ak_business_locations_workspace_company_branch_id", x => new { x.workspace_id, x.company_id, x.branch_id, x.id });
                    table.CheckConstraint("ck_business_locations_code", "code = normalized_code\nAND normalized_code ~ '^[A-Z0-9-]{2,32}$'");
                    table.CheckConstraint("ck_business_locations_status", "status IN (1, 2)");
                    table.CheckConstraint("ck_business_locations_type", "location_type IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("ck_business_locations_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_business_locations_branches_workspace_company_branch",
                        columns: x => new { x.workspace_id, x.company_id, x.branch_id },
                        principalSchema: "platform",
                        principalTable: "branches",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receiving_vehicles",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    normalized_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    registration_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    normalized_registration_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    vehicle_type = table.Column<short>(type: "smallint", nullable: false),
                    owner_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receiving_vehicles", x => x.id);
                    table.UniqueConstraint("ak_receiving_vehicles_workspace_company_id", x => new { x.workspace_id, x.company_id, x.id });
                    table.CheckConstraint("ck_receiving_vehicles_code", "code = normalized_code\nAND normalized_code ~ '^[A-Z0-9-]{2,32}$'");
                    table.CheckConstraint("ck_receiving_vehicles_registration", "normalized_registration_number ~\n    '^[A-Z0-9]{2,32}$'");
                    table.CheckConstraint("ck_receiving_vehicles_status", "status IN (1, 2)");
                    table.CheckConstraint("ck_receiving_vehicles_type", "vehicle_type IN (1, 2, 3, 4)");
                    table.CheckConstraint("ck_receiving_vehicles_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_receiving_vehicles_company",
                        columns: x => new { x.workspace_id, x.company_id },
                        principalSchema: "platform",
                        principalTable: "companies",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "weight_processing_policies",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    normalized_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    decimal_places = table.Column<int>(type: "integer", nullable: false),
                    processing_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weight_processing_policies", x => x.id);
                    table.UniqueConstraint("ak_weight_processing_policies_workspace_company_id", x => new { x.workspace_id, x.company_id, x.id });
                    table.CheckConstraint("ck_weight_processing_policies_code", "code = normalized_code\nAND normalized_code ~ '^[A-Z0-9-]{2,32}$'");
                    table.CheckConstraint("ck_weight_processing_policies_decimal_places", "decimal_places IN (1, 2, 3)");
                    table.CheckConstraint("ck_weight_processing_policies_method", "processing_method IN ('Standard', 'Floor', 'Ceiling')");
                    table.CheckConstraint("ck_weight_processing_policies_status", "status IN (1, 2)");
                    table.CheckConstraint("ck_weight_processing_policies_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_weight_processing_policies_company",
                        columns: x => new { x.workspace_id, x.company_id },
                        principalSchema: "platform",
                        principalTable: "companies",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_procurement_settings",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_destination_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_weight_processing_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_selection_mode = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_procurement_settings", x => x.id);
                    table.CheckConstraint("ck_company_procurement_settings_vehicle_mode", "vehicle_selection_mode IN (1, 2)");
                    table.CheckConstraint("ck_company_procurement_settings_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_company_procurement_settings_company",
                        columns: x => new { x.workspace_id, x.company_id },
                        principalSchema: "platform",
                        principalTable: "companies",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_company_procurement_settings_default_branch",
                        columns: x => new { x.workspace_id, x.company_id, x.default_branch_id },
                        principalSchema: "platform",
                        principalTable: "branches",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_company_procurement_settings_default_destination",
                        columns: x => new { x.workspace_id, x.company_id, x.default_branch_id, x.default_destination_location_id },
                        principalSchema: "operations",
                        principalTable: "business_locations",
                        principalColumns: new[] { "workspace_id", "company_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_company_procurement_settings_default_weight_policy",
                        columns: x => new { x.workspace_id, x.company_id, x.default_weight_processing_policy_id },
                        principalSchema: "procurement",
                        principalTable: "weight_processing_policies",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bag_types_company_status_code",
                schema: "procurement",
                table: "bag_types",
                columns: new[] { "workspace_id", "company_id", "status", "normalized_code", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_bag_types_workspace_company_code",
                schema: "procurement",
                table: "bag_types",
                columns: new[] { "workspace_id", "company_id", "normalized_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_business_locations_active_destination_lookup",
                schema: "operations",
                table: "business_locations",
                columns: new[] { "workspace_id", "company_id", "branch_id", "status", "normalized_code", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_business_locations_workspace_company_code",
                schema: "operations",
                table: "business_locations",
                columns: new[] { "workspace_id", "company_id", "normalized_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_procurement_settings_workspace_id_company_id_defau~1",
                schema: "procurement",
                table: "company_procurement_settings",
                columns: new[] { "workspace_id", "company_id", "default_branch_id", "default_destination_location_id" });

            migrationBuilder.CreateIndex(
                name: "IX_company_procurement_settings_workspace_id_company_id_defaul~",
                schema: "procurement",
                table: "company_procurement_settings",
                columns: new[] { "workspace_id", "company_id", "default_weight_processing_policy_id" });

            migrationBuilder.CreateIndex(
                name: "ux_company_procurement_settings_workspace_company",
                schema: "procurement",
                table: "company_procurement_settings",
                columns: new[] { "workspace_id", "company_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_receiving_vehicles_company_status_code",
                schema: "procurement",
                table: "receiving_vehicles",
                columns: new[] { "workspace_id", "company_id", "status", "normalized_code", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_receiving_vehicles_workspace_company_code",
                schema: "procurement",
                table: "receiving_vehicles",
                columns: new[] { "workspace_id", "company_id", "normalized_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_receiving_vehicles_workspace_company_registration",
                schema: "procurement",
                table: "receiving_vehicles",
                columns: new[] { "workspace_id", "company_id", "normalized_registration_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weight_processing_policies_company_status_code",
                schema: "procurement",
                table: "weight_processing_policies",
                columns: new[] { "workspace_id", "company_id", "status", "normalized_code", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_weight_processing_policies_workspace_company_code",
                schema: "procurement",
                table: "weight_processing_policies",
                columns: new[] { "workspace_id", "company_id", "normalized_code" },
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION operations.protect_business_locations()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      'business_locations_no_delete',
                                  MESSAGE =
                                      'Business locations cannot be physically deleted.';
                    END IF;

                    IF NEW.id IS DISTINCT FROM OLD.id
                       OR NEW.workspace_id IS DISTINCT FROM OLD.workspace_id
                       OR NEW.company_id IS DISTINCT FROM OLD.company_id
                       OR NEW.branch_id IS DISTINCT FROM OLD.branch_id
                       OR NEW.code IS DISTINCT FROM OLD.code
                       OR NEW.normalized_code IS DISTINCT FROM
                            OLD.normalized_code
                       OR NEW.created_at_utc IS DISTINCT FROM
                            OLD.created_at_utc THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      'business_locations_immutable_identity',
                                  MESSAGE =
                                      'Business location ownership, code, and creation facts are immutable.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_business_locations_protect
                BEFORE UPDATE OR DELETE
                ON operations.business_locations
                FOR EACH ROW
                EXECUTE FUNCTION operations.protect_business_locations();

                CREATE OR REPLACE FUNCTION
                    procurement.protect_commercial_master_identity()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      TG_TABLE_NAME || '_no_delete',
                                  MESSAGE =
                                      'Commercial master records cannot be physically deleted.';
                    END IF;

                    IF NEW.id IS DISTINCT FROM OLD.id
                       OR NEW.workspace_id IS DISTINCT FROM OLD.workspace_id
                       OR NEW.company_id IS DISTINCT FROM OLD.company_id
                       OR NEW.code IS DISTINCT FROM OLD.code
                       OR NEW.normalized_code IS DISTINCT FROM
                            OLD.normalized_code
                       OR NEW.created_at_utc IS DISTINCT FROM
                            OLD.created_at_utc THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      TG_TABLE_NAME ||
                                      '_immutable_identity',
                                  MESSAGE =
                                      'Commercial master ownership, code, and creation facts are immutable.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_receiving_vehicles_protect
                BEFORE UPDATE OR DELETE
                ON procurement.receiving_vehicles
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.protect_commercial_master_identity();

                CREATE TRIGGER tr_bag_types_protect
                BEFORE UPDATE OR DELETE
                ON procurement.bag_types
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.protect_commercial_master_identity();

                CREATE TRIGGER tr_weight_processing_policies_protect
                BEFORE UPDATE OR DELETE
                ON procurement.weight_processing_policies
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.protect_commercial_master_identity();

                CREATE OR REPLACE FUNCTION
                    procurement.protect_company_procurement_settings()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      'company_procurement_settings_no_delete',
                                  MESSAGE =
                                      'Company procurement settings cannot be physically deleted.';
                    END IF;

                    IF NEW.id IS DISTINCT FROM OLD.id
                       OR NEW.workspace_id IS DISTINCT FROM OLD.workspace_id
                       OR NEW.company_id IS DISTINCT FROM OLD.company_id
                       OR NEW.default_branch_id IS DISTINCT FROM
                            OLD.default_branch_id
                       OR NEW.created_at_utc IS DISTINCT FROM
                            OLD.created_at_utc THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      'company_procurement_settings_immutable_identity',
                                  MESSAGE =
                                      'Company procurement settings ownership, default branch, and creation facts are immutable.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_company_procurement_settings_protect
                BEFORE UPDATE OR DELETE
                ON procurement.company_procurement_settings
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.protect_company_procurement_settings();

                CREATE OR REPLACE FUNCTION
                    procurement.validate_company_procurement_settings()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM platform.branches AS branch
                        WHERE branch.workspace_id = NEW.workspace_id
                          AND branch.company_id = NEW.company_id
                          AND branch.id = NEW.default_branch_id
                          AND branch.is_default
                          AND branch.status = 1
                    )
                    OR NOT EXISTS (
                        SELECT 1
                        FROM operations.business_locations AS location
                        WHERE location.workspace_id = NEW.workspace_id
                          AND location.company_id = NEW.company_id
                          AND location.branch_id = NEW.default_branch_id
                          AND location.id =
                              NEW.default_destination_location_id
                          AND location.status = 1
                    )
                    OR NOT EXISTS (
                        SELECT 1
                        FROM procurement.weight_processing_policies AS policy
                        WHERE policy.workspace_id = NEW.workspace_id
                          AND policy.company_id = NEW.company_id
                          AND policy.id =
                              NEW.default_weight_processing_policy_id
                          AND policy.status = 1
                    ) THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '23514',
                                  CONSTRAINT =
                                      'ck_company_procurement_settings_active_defaults',
                                  MESSAGE =
                                      'Procurement defaults must reference the active default branch, an active branch location, and an active weight policy.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_company_procurement_settings_validate
                BEFORE INSERT OR UPDATE
                ON procurement.company_procurement_settings
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.validate_company_procurement_settings();

                CREATE OR REPLACE FUNCTION
                    operations.prevent_default_location_deactivation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF OLD.status = 1
                       AND NEW.status = 2
                       AND EXISTS (
                           SELECT 1
                           FROM procurement.company_procurement_settings
                           AS settings
                           WHERE settings.workspace_id = OLD.workspace_id
                             AND settings.company_id = OLD.company_id
                             AND settings.default_branch_id = OLD.branch_id
                             AND settings.default_destination_location_id =
                                 OLD.id
                       ) THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '23514',
                                  CONSTRAINT =
                                      'ck_procurement_default_location_active',
                                  MESSAGE =
                                      'The default procurement destination cannot be deactivated.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER
                    tr_business_locations_prevent_default_deactivation
                BEFORE UPDATE OF status
                ON operations.business_locations
                FOR EACH ROW
                EXECUTE FUNCTION
                    operations.prevent_default_location_deactivation();

                CREATE OR REPLACE FUNCTION
                    procurement.prevent_default_weight_policy_deactivation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF OLD.status = 1
                       AND NEW.status = 2
                       AND EXISTS (
                           SELECT 1
                           FROM procurement.company_procurement_settings
                           AS settings
                           WHERE settings.workspace_id = OLD.workspace_id
                             AND settings.company_id = OLD.company_id
                             AND
                                settings.default_weight_processing_policy_id =
                                    OLD.id
                       ) THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '23514',
                                  CONSTRAINT =
                                      'ck_procurement_default_weight_policy_active',
                                  MESSAGE =
                                      'The default procurement weight policy cannot be deactivated.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER
                    tr_weight_policies_prevent_default_deactivation
                BEFORE UPDATE OF status
                ON procurement.weight_processing_policies
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.prevent_default_weight_policy_deactivation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP FUNCTION IF EXISTS
                    procurement.prevent_default_weight_policy_deactivation()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    operations.prevent_default_location_deactivation()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    procurement.validate_company_procurement_settings()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    procurement.protect_company_procurement_settings()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    procurement.protect_commercial_master_identity()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    operations.protect_business_locations()
                    CASCADE;
                """);

            migrationBuilder.DropTable(
                name: "bag_types",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "company_procurement_settings",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "receiving_vehicles",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "business_locations",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "weight_processing_policies",
                schema: "procurement");
        }
    }
}
