using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenCommercialOperationalMasterDataContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_receiving_vehicles_registration",
                schema: "procurement",
                table: "receiving_vehicles");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receiving_vehicles_registration",
                schema: "procurement",
                table: "receiving_vehicles",
                sql: "registration_number = btrim(registration_number)\nAND normalized_registration_number ~\n    '^[A-Z0-9]{2,32}$'\nAND normalized_registration_number = upper(\n    regexp_replace(\n        registration_number,\n        '[ -]',\n        '',\n        'g'))");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION
                    platform.enforce_commercial_master_revision()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    old_business_values jsonb;
                    new_business_values jsonb;
                BEGIN
                    IF NEW.version <> OLD.version + 1 THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      TG_TABLE_NAME ||
                                      '_revision_transition',
                                  MESSAGE =
                                      'Commercial master updates must advance Version exactly once.';
                    END IF;

                    IF NEW.updated_at_utc < OLD.updated_at_utc THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      TG_TABLE_NAME ||
                                      '_revision_timestamp',
                                  MESSAGE =
                                      'Commercial master UpdatedAtUtc cannot move backwards.';
                    END IF;

                    IF NEW.created_at_utc IS DISTINCT FROM
                        OLD.created_at_utc THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      TG_TABLE_NAME ||
                                      '_immutable_creation',
                                  MESSAGE =
                                      'Commercial master creation time is immutable.';
                    END IF;

                    old_business_values := to_jsonb(OLD) - ARRAY[
                        'id', 'workspace_id', 'company_id', 'branch_id',
                        'default_branch_id', 'code', 'normalized_code',
                        'created_at_utc', 'updated_at_utc', 'version'];
                    new_business_values := to_jsonb(NEW) - ARRAY[
                        'id', 'workspace_id', 'company_id', 'branch_id',
                        'default_branch_id', 'code', 'normalized_code',
                        'created_at_utc', 'updated_at_utc', 'version'];
                    IF new_business_values = old_business_values THEN
                        RAISE EXCEPTION
                            USING ERRCODE = '55000',
                                  CONSTRAINT =
                                      TG_TABLE_NAME ||
                                      '_revision_payload_required',
                                  MESSAGE =
                                      'Commercial master revisions require a material mutable-field change.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER tr_business_locations_revision_contract
                BEFORE UPDATE ON operations.business_locations
                FOR EACH ROW
                EXECUTE FUNCTION
                    platform.enforce_commercial_master_revision();

                CREATE TRIGGER tr_receiving_vehicles_revision_contract
                BEFORE UPDATE ON procurement.receiving_vehicles
                FOR EACH ROW
                EXECUTE FUNCTION
                    platform.enforce_commercial_master_revision();

                CREATE TRIGGER tr_bag_types_revision_contract
                BEFORE UPDATE ON procurement.bag_types
                FOR EACH ROW
                EXECUTE FUNCTION
                    platform.enforce_commercial_master_revision();

                CREATE TRIGGER
                    tr_weight_processing_policies_revision_contract
                BEFORE UPDATE ON procurement.weight_processing_policies
                FOR EACH ROW
                EXECUTE FUNCTION
                    platform.enforce_commercial_master_revision();

                CREATE TRIGGER
                    tr_company_procurement_settings_revision_contract
                BEFORE UPDATE ON procurement.company_procurement_settings
                FOR EACH ROW
                EXECUTE FUNCTION
                    platform.enforce_commercial_master_revision();

                CREATE OR REPLACE FUNCTION
                    procurement.lock_procurement_defaults(
                        target_workspace_id uuid,
                        target_company_id uuid)
                RETURNS void
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    PERFORM pg_advisory_xact_lock(
                        hashtextextended(
                            'TraderPro.ProcurementDefaults.v1' || E'\n' ||
                            target_workspace_id::text || E'\n' ||
                            target_company_id::text,
                            0));
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    procurement.validate_company_procurement_settings()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    PERFORM procurement.lock_procurement_defaults(
                        NEW.workspace_id,
                        NEW.company_id);

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

                CREATE OR REPLACE FUNCTION
                    operations.prevent_default_location_deactivation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF OLD.status = 1 AND NEW.status = 2 THEN
                        PERFORM procurement.lock_procurement_defaults(
                            OLD.workspace_id,
                            OLD.company_id);

                        IF EXISTS (
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
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE OR REPLACE FUNCTION
                    procurement.prevent_default_weight_policy_deactivation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF OLD.status = 1 AND NEW.status = 2 THEN
                        PERFORM procurement.lock_procurement_defaults(
                            OLD.workspace_id,
                            OLD.company_id);

                        IF EXISTS (
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
                    END IF;

                    RETURN NEW;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS
                    tr_company_procurement_settings_revision_contract
                    ON procurement.company_procurement_settings;
                DROP TRIGGER IF EXISTS
                    tr_weight_processing_policies_revision_contract
                    ON procurement.weight_processing_policies;
                DROP TRIGGER IF EXISTS
                    tr_bag_types_revision_contract
                    ON procurement.bag_types;
                DROP TRIGGER IF EXISTS
                    tr_receiving_vehicles_revision_contract
                    ON procurement.receiving_vehicles;
                DROP TRIGGER IF EXISTS
                    tr_business_locations_revision_contract
                    ON operations.business_locations;
                DROP FUNCTION IF EXISTS
                    platform.enforce_commercial_master_revision();

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

                DROP FUNCTION IF EXISTS
                    procurement.lock_procurement_defaults(uuid, uuid);
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_receiving_vehicles_registration",
                schema: "procurement",
                table: "receiving_vehicles");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receiving_vehicles_registration",
                schema: "procurement",
                table: "receiving_vehicles",
                sql: "normalized_registration_number ~\n    '^[A-Z0-9]{2,32}$'");
        }
    }
}
