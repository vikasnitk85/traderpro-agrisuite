using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialSupplierAndProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "product_groups",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    normalized_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    local_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_groups", x => x.id);
                    table.UniqueConstraint("ak_product_groups_workspace_company_id", x => new { x.workspace_id, x.company_id, x.id });
                    table.CheckConstraint("ck_product_groups_code", "code = normalized_code\nAND normalized_code ~ '^[A-Z0-9-]{2,32}$'");
                    table.CheckConstraint("ck_product_groups_status", "status IN (1, 2)");
                    table.CheckConstraint("ck_product_groups_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_product_groups_company",
                        columns: x => new { x.workspace_id, x.company_id },
                        principalSchema: "platform",
                        principalTable: "companies",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
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
                    supplier_type = table.Column<short>(type: "smallint", nullable: false),
                    product_scope_mode = table.Column<short>(type: "smallint", nullable: false),
                    contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tax_registration_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    normalized_tax_registration_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                    table.UniqueConstraint("ak_suppliers_workspace_company_id", x => new { x.workspace_id, x.company_id, x.id });
                    table.CheckConstraint("ck_suppliers_code", "code = normalized_code\nAND normalized_code ~ '^[A-Z0-9-]{2,32}$'");
                    table.CheckConstraint("ck_suppliers_email", "email IS NULL OR (email = lower(btrim(email)) AND position('@' in email) > 1)");
                    table.CheckConstraint("ck_suppliers_scope_mode", "product_scope_mode IN (1, 2)");
                    table.CheckConstraint("ck_suppliers_status", "status IN (1, 2)");
                    table.CheckConstraint("ck_suppliers_tax_registration", "(tax_registration_number IS NULL AND normalized_tax_registration_number IS NULL) OR (tax_registration_number = btrim(tax_registration_number) AND normalized_tax_registration_number ~ '^[A-Z0-9]{2,64}$' AND normalized_tax_registration_number = upper(regexp_replace(tax_registration_number, '[ -]', '', 'g')))");
                    table.CheckConstraint("ck_suppliers_type", "supplier_type IN (1, 2)");
                    table.CheckConstraint("ck_suppliers_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_suppliers_company",
                        columns: x => new { x.workspace_id, x.company_id },
                        principalSchema: "platform",
                        principalTable: "companies",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    normalized_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    local_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    product_type = table.Column<short>(type: "smallint", nullable: false),
                    is_purchasable = table.Column<bool>(type: "boolean", nullable: false),
                    processing_family_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    normalized_processing_family_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.UniqueConstraint("ak_products_workspace_company_id", x => new { x.workspace_id, x.company_id, x.id });
                    table.CheckConstraint("ck_products_code", "code = normalized_code\nAND normalized_code ~ '^[A-Z0-9-]{2,32}$'");
                    table.CheckConstraint("ck_products_processing_family", "(processing_family_code IS NULL AND normalized_processing_family_code IS NULL) OR (processing_family_code = btrim(processing_family_code) AND normalized_processing_family_code ~ '^[A-Z0-9-]{2,32}$' AND normalized_processing_family_code = upper(processing_family_code))");
                    table.CheckConstraint("ck_products_status", "status IN (1, 2)");
                    table.CheckConstraint("ck_products_type", "product_type IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("ck_products_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_products_company",
                        columns: x => new { x.workspace_id, x.company_id },
                        principalSchema: "platform",
                        principalTable: "companies",
                        principalColumns: new[] { "workspace_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_product_group",
                        columns: x => new { x.workspace_id, x.company_id, x.product_group_id },
                        principalSchema: "catalog",
                        principalTable: "product_groups",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_standard_bag_weights",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bag_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    standard_content_weight_kg = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_standard_bag_weights", x => x.id);
                    table.UniqueConstraint("ak_product_standard_bag_weights_workspace_company_id", x => new { x.workspace_id, x.company_id, x.id });
                    table.CheckConstraint("ck_product_standard_bag_weights_default", "NOT is_default OR status = 1");
                    table.CheckConstraint("ck_product_standard_bag_weights_status", "status IN (1, 2)");
                    table.CheckConstraint("ck_product_standard_bag_weights_value", "standard_content_weight_kg > 0");
                    table.CheckConstraint("ck_product_standard_bag_weights_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_product_standard_bag_weights_bag_type",
                        columns: x => new { x.workspace_id, x.company_id, x.bag_type_id },
                        principalSchema: "procurement",
                        principalTable: "bag_types",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_standard_bag_weights_product",
                        columns: x => new { x.workspace_id, x.company_id, x.product_id },
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_product_scopes",
                schema: "procurement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_product_scopes", x => x.id);
                    table.UniqueConstraint("ak_supplier_product_scopes_workspace_company_id", x => new { x.workspace_id, x.company_id, x.id });
                    table.CheckConstraint("ck_supplier_product_scopes_status", "status IN (1, 2)");
                    table.CheckConstraint("ck_supplier_product_scopes_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_supplier_product_scopes_product",
                        columns: x => new { x.workspace_id, x.company_id, x.product_id },
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_product_scopes_supplier",
                        columns: x => new { x.workspace_id, x.company_id, x.supplier_id },
                        principalSchema: "procurement",
                        principalTable: "suppliers",
                        principalColumns: new[] { "workspace_id", "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_groups_company_status_code",
                schema: "catalog",
                table: "product_groups",
                columns: new[] { "workspace_id", "company_id", "status", "normalized_code", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_product_groups_workspace_company_code",
                schema: "catalog",
                table: "product_groups",
                columns: new[] { "workspace_id", "company_id", "normalized_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_standard_bag_weights_bag_status",
                schema: "catalog",
                table: "product_standard_bag_weights",
                columns: new[] { "workspace_id", "company_id", "bag_type_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_product_standard_bag_weights_product_status",
                schema: "catalog",
                table: "product_standard_bag_weights",
                columns: new[] { "workspace_id", "company_id", "product_id", "status", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_product_standard_bag_weights_active_default",
                schema: "catalog",
                table: "product_standard_bag_weights",
                columns: new[] { "workspace_id", "company_id", "product_id" },
                unique: true,
                filter: "status = 1 AND is_default");

            migrationBuilder.CreateIndex(
                name: "ux_product_standard_bag_weights_identity",
                schema: "catalog",
                table: "product_standard_bag_weights",
                columns: new[] { "workspace_id", "company_id", "product_id", "bag_type_id", "standard_content_weight_kg" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_group_status",
                schema: "catalog",
                table: "products",
                columns: new[] { "workspace_id", "company_id", "product_group_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_products_processing_family",
                schema: "catalog",
                table: "products",
                columns: new[] { "workspace_id", "company_id", "normalized_processing_family_code", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_products_receiving_selection",
                schema: "catalog",
                table: "products",
                columns: new[] { "workspace_id", "company_id", "status", "is_purchasable", "normalized_code", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_products_workspace_company_code",
                schema: "catalog",
                table: "products",
                columns: new[] { "workspace_id", "company_id", "normalized_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_product_scopes_product_status",
                schema: "procurement",
                table: "supplier_product_scopes",
                columns: new[] { "workspace_id", "company_id", "product_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_product_scopes_supplier_status",
                schema: "procurement",
                table: "supplier_product_scopes",
                columns: new[] { "workspace_id", "company_id", "supplier_id", "status", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_supplier_product_scopes_identity",
                schema: "procurement",
                table: "supplier_product_scopes",
                columns: new[] { "workspace_id", "company_id", "supplier_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_company_status_code",
                schema: "procurement",
                table: "suppliers",
                columns: new[] { "workspace_id", "company_id", "status", "normalized_code", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_suppliers_workspace_company_code",
                schema: "procurement",
                table: "suppliers",
                columns: new[] { "workspace_id", "company_id", "normalized_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_suppliers_workspace_company_tax",
                schema: "procurement",
                table: "suppliers",
                columns: new[] { "workspace_id", "company_id", "normalized_tax_registration_number" },
                unique: true,
                filter: "normalized_tax_registration_number IS NOT NULL");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION
                    procurement.lock_supplier_product_scope(
                        target_workspace_id uuid,
                        target_company_id uuid,
                        target_supplier_id uuid)
                RETURNS void
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    PERFORM pg_advisory_xact_lock(
                        hashtextextended(
                            'TraderPro.SupplierProductScope.v1' || E'\n' ||
                            target_workspace_id::text || E'\n' ||
                            target_company_id::text || E'\n' ||
                            target_supplier_id::text,
                            0));
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    catalog.lock_product_bag_default(
                        target_workspace_id uuid,
                        target_company_id uuid,
                        target_product_id uuid)
                RETURNS void
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    PERFORM pg_advisory_xact_lock(
                        hashtextextended(
                            'TraderPro.ProductBagDefault.v1' || E'\n' ||
                            target_workspace_id::text || E'\n' ||
                            target_company_id::text || E'\n' ||
                            target_product_id::text,
                            0));
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    catalog.lock_product_group_usage(
                        target_workspace_id uuid,
                        target_company_id uuid,
                        target_group_id uuid)
                RETURNS void
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    PERFORM pg_advisory_xact_lock(
                        hashtextextended(
                            'TraderPro.ProductGroupUsage.v1' || E'\n' ||
                            target_workspace_id::text || E'\n' ||
                            target_company_id::text || E'\n' ||
                            target_group_id::text,
                            0));
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    catalog.lock_bag_type_usage(
                        target_workspace_id uuid,
                        target_company_id uuid,
                        target_bag_type_id uuid)
                RETURNS void
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    PERFORM pg_advisory_xact_lock(
                        hashtextextended(
                            'TraderPro.BagTypeUsage.v1' || E'\n' ||
                            target_workspace_id::text || E'\n' ||
                            target_company_id::text || E'\n' ||
                            target_bag_type_id::text,
                            0));
                END;
                $$;

                CREATE OR REPLACE FUNCTION catalog.protect_catalog_master()
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
                                      'Catalog masters cannot be physically deleted.';
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
                                      TG_TABLE_NAME || '_immutable_identity',
                                  MESSAGE =
                                      'Catalog master ownership, code, and creation facts are immutable.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    catalog.protect_catalog_association()
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
                                      'Catalog associations cannot be physically deleted.';
                    END IF;

                    IF NEW.id IS DISTINCT FROM OLD.id
                       OR NEW.workspace_id IS DISTINCT FROM OLD.workspace_id
                       OR NEW.company_id IS DISTINCT FROM OLD.company_id
                       OR NEW.created_at_utc IS DISTINCT FROM
                            OLD.created_at_utc
                       OR (
                            TG_TABLE_NAME =
                                'product_standard_bag_weights'
                            AND (
                                NEW.product_id IS DISTINCT FROM OLD.product_id
                                OR NEW.bag_type_id IS DISTINCT FROM
                                    OLD.bag_type_id)) THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      TG_TABLE_NAME || '_immutable_identity',
                                  MESSAGE =
                                      'Catalog association ownership and identity are immutable.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    procurement.protect_supplier_product_scope()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      'supplier_product_scopes_no_delete',
                                  MESSAGE =
                                      'Supplier product scopes cannot be physically deleted.';
                    END IF;

                    IF NEW.id IS DISTINCT FROM OLD.id
                       OR NEW.workspace_id IS DISTINCT FROM OLD.workspace_id
                       OR NEW.company_id IS DISTINCT FROM OLD.company_id
                       OR NEW.supplier_id IS DISTINCT FROM OLD.supplier_id
                       OR NEW.product_id IS DISTINCT FROM OLD.product_id
                       OR NEW.created_at_utc IS DISTINCT FROM
                            OLD.created_at_utc THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      'supplier_product_scopes_immutable_identity',
                                  MESSAGE =
                                      'Supplier product scope ownership and association identity are immutable.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_suppliers_protect
                BEFORE UPDATE OR DELETE ON procurement.suppliers
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.protect_commercial_master_identity();

                CREATE TRIGGER tr_product_groups_protect
                BEFORE UPDATE OR DELETE ON catalog.product_groups
                FOR EACH ROW
                EXECUTE FUNCTION catalog.protect_catalog_master();

                CREATE TRIGGER tr_products_protect
                BEFORE UPDATE OR DELETE ON catalog.products
                FOR EACH ROW
                EXECUTE FUNCTION catalog.protect_catalog_master();

                CREATE TRIGGER tr_product_standard_bag_weights_protect
                BEFORE UPDATE OR DELETE
                ON catalog.product_standard_bag_weights
                FOR EACH ROW
                EXECUTE FUNCTION catalog.protect_catalog_association();

                CREATE TRIGGER tr_supplier_product_scopes_protect
                BEFORE UPDATE OR DELETE
                ON procurement.supplier_product_scopes
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.protect_supplier_product_scope();

                CREATE TRIGGER tr_suppliers_revision_contract
                BEFORE UPDATE ON procurement.suppliers
                FOR EACH ROW
                EXECUTE FUNCTION
                    platform.enforce_commercial_master_revision();

                CREATE TRIGGER tr_product_groups_revision_contract
                BEFORE UPDATE ON catalog.product_groups
                FOR EACH ROW
                EXECUTE FUNCTION
                    platform.enforce_commercial_master_revision();

                CREATE TRIGGER tr_products_revision_contract
                BEFORE UPDATE ON catalog.products
                FOR EACH ROW
                EXECUTE FUNCTION
                    platform.enforce_commercial_master_revision();

                CREATE TRIGGER
                    tr_product_standard_bag_weights_revision_contract
                BEFORE UPDATE ON catalog.product_standard_bag_weights
                FOR EACH ROW
                EXECUTE FUNCTION
                    platform.enforce_commercial_master_revision();

                CREATE TRIGGER
                    tr_supplier_product_scopes_revision_contract
                BEFORE UPDATE ON procurement.supplier_product_scopes
                FOR EACH ROW
                EXECUTE FUNCTION
                    platform.enforce_commercial_master_revision();

                CREATE OR REPLACE FUNCTION
                    catalog.validate_active_product_group()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    PERFORM catalog.lock_product_group_usage(
                        NEW.workspace_id,
                        NEW.company_id,
                        NEW.product_group_id);

                    IF NEW.status = 1 AND NOT EXISTS (
                        SELECT 1
                        FROM catalog.product_groups AS product_group
                        WHERE product_group.workspace_id = NEW.workspace_id
                          AND product_group.company_id = NEW.company_id
                          AND product_group.id = NEW.product_group_id
                          AND product_group.status = 1
                    ) THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '23514',
                                  CONSTRAINT = 'ck_product_group_active',
                                  MESSAGE =
                                      'An active Product requires an active same-company Product Group.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_products_validate_group
                BEFORE INSERT OR UPDATE ON catalog.products
                FOR EACH ROW
                EXECUTE FUNCTION catalog.validate_active_product_group();

                CREATE OR REPLACE FUNCTION
                    catalog.prevent_product_group_deactivation_in_use()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF OLD.status = 1 AND NEW.status = 2 THEN
                        PERFORM catalog.lock_product_group_usage(
                            OLD.workspace_id,
                            OLD.company_id,
                            OLD.id);
                        IF EXISTS (
                            SELECT 1
                            FROM catalog.products AS product
                            WHERE product.workspace_id = OLD.workspace_id
                              AND product.company_id = OLD.company_id
                              AND product.product_group_id = OLD.id
                              AND product.status = 1
                        ) THEN
                            RAISE EXCEPTION
                                USING ERRCODE = '23514',
                                      CONSTRAINT =
                                          'ck_product_group_in_use',
                                      MESSAGE =
                                          'A Product Group with active Products cannot be deactivated.';
                        END IF;
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_product_groups_in_use
                BEFORE UPDATE OF status ON catalog.product_groups
                FOR EACH ROW
                EXECUTE FUNCTION
                    catalog.prevent_product_group_deactivation_in_use();

                CREATE OR REPLACE FUNCTION
                    catalog.prevent_product_deactivation_in_use()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF (OLD.status = 1 AND NEW.status = 2)
                       OR (OLD.is_purchasable AND NOT NEW.is_purchasable) THEN
                        PERFORM catalog.lock_product_bag_default(
                            OLD.workspace_id,
                            OLD.company_id,
                            OLD.id);
                        IF EXISTS (
                            SELECT 1
                            FROM procurement.supplier_product_scopes AS scope
                            WHERE scope.workspace_id = OLD.workspace_id
                              AND scope.company_id = OLD.company_id
                              AND scope.product_id = OLD.id
                              AND scope.status = 1
                        ) THEN
                            RAISE EXCEPTION
                                USING ERRCODE = '23514',
                                      CONSTRAINT =
                                          'ck_product_supplier_scope_in_use',
                                      MESSAGE =
                                          'Active Supplier Product Scopes must be deactivated first.';
                        END IF;
                        IF EXISTS (
                            SELECT 1
                            FROM catalog.product_standard_bag_weights
                                AS standard
                            WHERE standard.workspace_id = OLD.workspace_id
                              AND standard.company_id = OLD.company_id
                              AND standard.product_id = OLD.id
                              AND standard.status = 1
                        ) THEN
                            RAISE EXCEPTION
                                USING ERRCODE = '23514',
                                      CONSTRAINT =
                                          'ck_product_standard_bag_weight_in_use',
                                      MESSAGE =
                                          'Active Product Standard Bag Weights must be deactivated first.';
                        END IF;
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_products_in_use
                BEFORE UPDATE OF status, is_purchasable ON catalog.products
                FOR EACH ROW
                EXECUTE FUNCTION
                    catalog.prevent_product_deactivation_in_use();

                CREATE OR REPLACE FUNCTION
                    procurement.lock_supplier_scope_change()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    PERFORM procurement.lock_supplier_product_scope(
                        NEW.workspace_id,
                        NEW.company_id,
                        NEW.id);
                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_suppliers_scope_lock
                BEFORE INSERT OR UPDATE OF product_scope_mode
                ON procurement.suppliers
                FOR EACH ROW
                EXECUTE FUNCTION procurement.lock_supplier_scope_change();

                CREATE OR REPLACE FUNCTION
                    procurement.validate_supplier_product_scope()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    PERFORM procurement.lock_supplier_product_scope(
                        NEW.workspace_id,
                        NEW.company_id,
                        NEW.supplier_id);
                    PERFORM catalog.lock_product_bag_default(
                        NEW.workspace_id,
                        NEW.company_id,
                        NEW.product_id);

                    IF NEW.status = 1 AND (
                        NOT EXISTS (
                            SELECT 1
                            FROM procurement.suppliers AS supplier
                            WHERE supplier.workspace_id = NEW.workspace_id
                              AND supplier.company_id = NEW.company_id
                              AND supplier.id = NEW.supplier_id
                              AND supplier.status = 1)
                        OR NOT EXISTS (
                            SELECT 1
                            FROM catalog.products AS product
                            WHERE product.workspace_id = NEW.workspace_id
                              AND product.company_id = NEW.company_id
                              AND product.id = NEW.product_id
                              AND product.status = 1
                              AND product.is_purchasable)
                    ) THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '23514',
                                  CONSTRAINT =
                                      'ck_supplier_product_scope_active_references',
                                  MESSAGE =
                                      'An active scope requires an active Supplier and active purchasable Product.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_supplier_product_scopes_validate
                BEFORE INSERT OR UPDATE
                ON procurement.supplier_product_scopes
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.validate_supplier_product_scope();

                CREATE OR REPLACE FUNCTION
                    procurement.enforce_restricted_supplier_scope()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    target_workspace_id uuid;
                    target_company_id uuid;
                    target_supplier_id uuid;
                BEGIN
                    IF TG_TABLE_NAME = 'suppliers' THEN
                        target_workspace_id := NEW.workspace_id;
                        target_company_id := NEW.company_id;
                        target_supplier_id := NEW.id;
                    ELSE
                        target_workspace_id := NEW.workspace_id;
                        target_company_id := NEW.company_id;
                        target_supplier_id := NEW.supplier_id;
                    END IF;

                    PERFORM procurement.lock_supplier_product_scope(
                        target_workspace_id,
                        target_company_id,
                        target_supplier_id);
                    IF EXISTS (
                        SELECT 1
                        FROM procurement.suppliers AS supplier
                        WHERE supplier.workspace_id = target_workspace_id
                          AND supplier.company_id = target_company_id
                          AND supplier.id = target_supplier_id
                          AND supplier.product_scope_mode = 2
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM procurement.supplier_product_scopes AS scope
                        WHERE scope.workspace_id = target_workspace_id
                          AND scope.company_id = target_company_id
                          AND scope.supplier_id = target_supplier_id
                          AND scope.status = 1
                    ) THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '23514',
                                  CONSTRAINT =
                                      'ck_supplier_product_scope_required',
                                  MESSAGE =
                                      'A Restricted Supplier requires an active Product Scope.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE CONSTRAINT TRIGGER tr_suppliers_scope_invariant
                AFTER INSERT OR UPDATE ON procurement.suppliers
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.enforce_restricted_supplier_scope();

                CREATE CONSTRAINT TRIGGER
                    tr_supplier_product_scopes_invariant
                AFTER INSERT OR UPDATE
                ON procurement.supplier_product_scopes
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.enforce_restricted_supplier_scope();

                CREATE OR REPLACE FUNCTION
                    catalog.validate_product_standard_bag_weight()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    PERFORM catalog.lock_product_bag_default(
                        NEW.workspace_id,
                        NEW.company_id,
                        NEW.product_id);
                    PERFORM catalog.lock_bag_type_usage(
                        NEW.workspace_id,
                        NEW.company_id,
                        NEW.bag_type_id);

                    IF NEW.status = 1 AND (
                        NOT EXISTS (
                            SELECT 1
                            FROM catalog.products AS product
                            WHERE product.workspace_id = NEW.workspace_id
                              AND product.company_id = NEW.company_id
                              AND product.id = NEW.product_id
                              AND product.status = 1
                              AND product.is_purchasable)
                        OR NOT EXISTS (
                            SELECT 1
                            FROM procurement.bag_types AS bag_type
                            WHERE bag_type.workspace_id = NEW.workspace_id
                              AND bag_type.company_id = NEW.company_id
                              AND bag_type.id = NEW.bag_type_id
                              AND bag_type.status = 1)
                    ) THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '23514',
                                  CONSTRAINT =
                                      'ck_product_standard_bag_weight_active_references',
                                  MESSAGE =
                                      'An active standard requires an active purchasable Product and active Bag Type.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_product_standard_bag_weights_validate
                BEFORE INSERT OR UPDATE
                ON catalog.product_standard_bag_weights
                FOR EACH ROW
                EXECUTE FUNCTION
                    catalog.validate_product_standard_bag_weight();

                CREATE OR REPLACE FUNCTION
                    catalog.prevent_bag_type_deactivation_in_use()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF OLD.status = 1 AND NEW.status = 2 THEN
                        PERFORM catalog.lock_bag_type_usage(
                            OLD.workspace_id,
                            OLD.company_id,
                            OLD.id);
                        IF EXISTS (
                            SELECT 1
                            FROM catalog.product_standard_bag_weights
                                AS standard
                            WHERE standard.workspace_id = OLD.workspace_id
                              AND standard.company_id = OLD.company_id
                              AND standard.bag_type_id = OLD.id
                              AND standard.status = 1
                        ) THEN
                            RAISE EXCEPTION
                                USING ERRCODE = '23514',
                                      CONSTRAINT =
                                          'ck_bag_type_product_standard_in_use',
                                      MESSAGE =
                                          'Active Product Standard Bag Weights must be deactivated first.';
                        END IF;
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER
                    tr_bag_types_prevent_product_standard_deactivation
                BEFORE UPDATE OF status ON procurement.bag_types
                FOR EACH ROW
                EXECUTE FUNCTION
                    catalog.prevent_bag_type_deactivation_in_use();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS
                    tr_bag_types_prevent_product_standard_deactivation
                    ON procurement.bag_types;
                DROP FUNCTION IF EXISTS
                    catalog.prevent_bag_type_deactivation_in_use();
                DROP FUNCTION IF EXISTS
                    catalog.validate_product_standard_bag_weight()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    procurement.enforce_restricted_supplier_scope()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    procurement.validate_supplier_product_scope()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    procurement.lock_supplier_scope_change()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    catalog.prevent_product_deactivation_in_use()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    catalog.prevent_product_group_deactivation_in_use()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    catalog.validate_active_product_group()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    procurement.protect_supplier_product_scope()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    catalog.protect_catalog_association()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    catalog.protect_catalog_master()
                    CASCADE;
                DROP FUNCTION IF EXISTS
                    catalog.lock_bag_type_usage(uuid, uuid, uuid);
                DROP FUNCTION IF EXISTS
                    catalog.lock_product_group_usage(uuid, uuid, uuid);
                DROP FUNCTION IF EXISTS
                    catalog.lock_product_bag_default(uuid, uuid, uuid);
                DROP FUNCTION IF EXISTS
                    procurement.lock_supplier_product_scope(
                        uuid,
                        uuid,
                        uuid);
                """);

            migrationBuilder.DropTable(
                name: "product_standard_bag_weights",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "supplier_product_scopes",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "products",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "procurement");

            migrationBuilder.DropTable(
                name: "product_groups",
                schema: "catalog");
        }
    }
}
