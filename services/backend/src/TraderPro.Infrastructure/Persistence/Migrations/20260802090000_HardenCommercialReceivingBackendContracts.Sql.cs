namespace TraderPro.Infrastructure.Persistence.Migrations;

public partial class HardenCommercialReceivingBackendContracts
{
    private const string BackfillLegacyReceivingSql =
        """
        INSERT INTO sync.commercial_receiving_operation_claims(
            workspace_id, company_id, command_scope, operation_id, operation_type,
            session_id, device_id, ownership_generation, request_hash, state,
            retryable, first_seen_at_utc, completed_at_utc, created_at_utc, updated_at_utc)
        SELECT s.workspace_id, s.company_id,
               'Procurement.CommercialReceiving.MobileSyncOperation', s.id,
               'LegacyCompletedCommercialReceivingSession', s.id, o.editor_device_id,
               o.ownership_generation, repeat('0', 64), 4, false,
               s.created_at_utc, s.updated_at_utc, s.created_at_utc, s.updated_at_utc
        FROM procurement.commercial_receiving_sessions s
        JOIN procurement.commercial_receiving_ownerships o
          ON o.workspace_id = s.workspace_id AND o.company_id = s.company_id
         AND o.receiving_session_id = s.id
        ON CONFLICT DO NOTHING;

        INSERT INTO procurement.commercial_receiving_reference_reservations(
            id, workspace_id, company_id, policy_id, period_key, operation_id,
            session_id, request_hash, policy_version, sequence, rendered_reference,
            reserved_at_utc, consumed_at_utc, created_at_utc)
        SELECT s.id, s.workspace_id, s.company_id, p.id, 'LEGACY', s.id, s.id,
               repeat('0', 64), s.reference_policy_version_snapshot,
               s.cloud_reference_sequence, s.cloud_reference,
               s.created_at_utc, s.created_at_utc, s.created_at_utc
        FROM procurement.commercial_receiving_sessions s
        JOIN procurement.commercial_receiving_reference_policies p
          ON p.workspace_id = s.workspace_id AND p.company_id = s.company_id
         AND p.document_type = 'CommercialReceiving'
        ON CONFLICT DO NOTHING;

        UPDATE procurement.commercial_receiving_sessions s
        SET reference_reservation_id = r.id,
            ownership_generation = o.ownership_generation
        FROM procurement.commercial_receiving_reference_reservations r,
             procurement.commercial_receiving_ownerships o
        WHERE r.workspace_id = s.workspace_id AND r.company_id = s.company_id
          AND r.session_id = s.id
          AND o.workspace_id = s.workspace_id AND o.company_id = s.company_id
          AND o.receiving_session_id = s.id;
        """;

    private const string IntegrityFunctionsSql =
        """
        ALTER TABLE procurement.commercial_receiving_sessions
            DROP CONSTRAINT ck_commercial_receiving_sessions_shape;
        ALTER TABLE procurement.commercial_receiving_sessions
            ADD CONSTRAINT ck_commercial_receiving_sessions_shape
            CHECK (version > 0 AND ownership_generation > 0 AND
                   next_expected_local_sequence >= 2 AND entry_count >= 0 AND
                   processed_total_weight_kg >= 0 AND cloud_reference_sequence > 0 AND
                   reference_policy_version_snapshot > 0 AND
                   ((status = 1 AND submitted_at_utc IS NULL) OR
                    (status = 2 AND submitted_at_utc IS NOT NULL AND entry_count > 0 AND processed_total_weight_kg > 0)));

        CREATE FUNCTION sync.guard_commercial_receiving_operation_claim()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        BEGIN
            IF TG_OP = 'DELETE' THEN
                RAISE EXCEPTION 'Commercial Receiving Operation claims cannot be deleted.' USING ERRCODE = '55000';
            END IF;
            IF ROW(NEW.workspace_id, NEW.company_id, NEW.command_scope, NEW.operation_id,
                   NEW.operation_type, NEW.session_id, NEW.device_id,
                   NEW.ownership_generation, NEW.request_hash, NEW.first_seen_at_utc,
                   NEW.created_at_utc)
               IS DISTINCT FROM
               ROW(OLD.workspace_id, OLD.company_id, OLD.command_scope, OLD.operation_id,
                   OLD.operation_type, OLD.session_id, OLD.device_id,
                   OLD.ownership_generation, OLD.request_hash, OLD.first_seen_at_utc,
                   OLD.created_at_utc)
            THEN
                RAISE EXCEPTION 'Commercial Receiving Operation claim identity is immutable.' USING ERRCODE = '55000';
            END IF;
            IF NEW.updated_at_utc < OLD.updated_at_utc OR
               (OLD.state = 4 AND NEW.state <> 4) OR
               (OLD.state = 3 AND NEW.state <> 3)
            THEN
                RAISE EXCEPTION 'Commercial Receiving Operation claim transition is invalid.' USING ERRCODE = '55000';
            END IF;
            RETURN NEW;
        END;
        $function$;
        CREATE TRIGGER tr_commercial_receiving_operation_claims_guard
        BEFORE UPDATE OR DELETE ON sync.commercial_receiving_operation_claims
        FOR EACH ROW EXECUTE FUNCTION sync.guard_commercial_receiving_operation_claim();

        CREATE FUNCTION procurement.guard_commercial_receiving_reference_reservation()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE claim_row record;
        BEGIN
            IF TG_OP = 'DELETE' THEN
                RAISE EXCEPTION 'Commercial Receiving reference reservations cannot be deleted.' USING ERRCODE = '55000';
            END IF;
            IF TG_OP = 'INSERT' THEN
                SELECT * INTO claim_row FROM sync.commercial_receiving_operation_claims
                WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id
                  AND operation_id = NEW.operation_id;
                IF NOT FOUND OR claim_row.operation_type <> 'StartCommercialReceivingSession' OR
                   claim_row.session_id <> NEW.session_id OR claim_row.request_hash <> NEW.request_hash THEN
                    RAISE EXCEPTION 'Commercial Receiving reference reservation does not match its Start claim.' USING ERRCODE = '55000';
                END IF;
                RETURN NEW;
            END IF;
            IF NEW.id <> OLD.id OR NEW.workspace_id <> OLD.workspace_id OR
               NEW.company_id <> OLD.company_id OR NEW.policy_id <> OLD.policy_id OR
               NEW.period_key <> OLD.period_key OR NEW.operation_id <> OLD.operation_id OR
               NEW.session_id <> OLD.session_id OR NEW.request_hash <> OLD.request_hash OR
               NEW.policy_version <> OLD.policy_version OR NEW.sequence <> OLD.sequence OR
               NEW.rendered_reference <> OLD.rendered_reference OR
               NEW.reserved_at_utc <> OLD.reserved_at_utc OR
               NEW.created_at_utc <> OLD.created_at_utc OR
               OLD.consumed_at_utc IS NOT NULL OR NEW.consumed_at_utc IS NULL OR
               NEW.consumed_at_utc < NEW.reserved_at_utc
            THEN
                RAISE EXCEPTION 'Commercial Receiving reference reservation transition is invalid.' USING ERRCODE = '55000';
            END IF;
            RETURN NEW;
        END;
        $function$;
        CREATE TRIGGER tr_commercial_receiving_reference_reservations_guard
        BEFORE INSERT OR UPDATE OR DELETE ON procurement.commercial_receiving_reference_reservations
        FOR EACH ROW EXECUTE FUNCTION procurement.guard_commercial_receiving_reference_reservation();

        CREATE FUNCTION procurement.validate_commercial_receiving_reservation_consumption()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE changed_row jsonb := to_jsonb(NEW);
        DECLARE reservation_key uuid := CASE WHEN TG_TABLE_NAME = 'commercial_receiving_sessions'
            THEN (changed_row ->> 'reference_reservation_id')::uuid
            ELSE (changed_row ->> 'id')::uuid END;
        DECLARE reservation_row record;
        DECLARE session_count bigint;
        BEGIN
            SELECT * INTO reservation_row
            FROM procurement.commercial_receiving_reference_reservations
            WHERE id = reservation_key;
            IF NOT FOUND THEN
                RAISE EXCEPTION 'Commercial Receiving Session requires a reference reservation.' USING ERRCODE = '55000';
            END IF;
            SELECT count(*) INTO session_count
            FROM procurement.commercial_receiving_sessions s
            WHERE s.workspace_id = reservation_row.workspace_id
              AND s.company_id = reservation_row.company_id
              AND s.reference_reservation_id = reservation_row.id
              AND s.id = reservation_row.session_id
              AND s.cloud_reference = reservation_row.rendered_reference
              AND s.cloud_reference_sequence = reservation_row.sequence
              AND s.reference_policy_version_snapshot = reservation_row.policy_version;
            IF (reservation_row.consumed_at_utc IS NULL AND session_count <> 0) OR
               (reservation_row.consumed_at_utc IS NOT NULL AND session_count <> 1)
            THEN
                RAISE EXCEPTION 'Commercial Receiving reference reservation consumption is inconsistent.' USING ERRCODE = '55000';
            END IF;
            RETURN NULL;
        END;
        $function$;
        CREATE CONSTRAINT TRIGGER tr_commercial_receiving_sessions_reservation
        AFTER INSERT OR UPDATE ON procurement.commercial_receiving_sessions
        DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
        EXECUTE FUNCTION procurement.validate_commercial_receiving_reservation_consumption();
        CREATE CONSTRAINT TRIGGER tr_commercial_receiving_reservations_consumption
        AFTER INSERT OR UPDATE ON procurement.commercial_receiving_reference_reservations
        DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
        EXECUTE FUNCTION procurement.validate_commercial_receiving_reservation_consumption();

        CREATE FUNCTION procurement.validate_commercial_receiving_session_snapshot()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE reservation_row record;
        DECLARE settings_row record;
        DECLARE supplier_row record;
        DECLARE destination_row record;
        DECLARE policy_row record;
        DECLARE vehicle_row record;
        BEGIN
            SELECT * INTO reservation_row FROM procurement.commercial_receiving_reference_reservations
            WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id
              AND id = NEW.reference_reservation_id;
            SELECT * INTO supplier_row FROM procurement.suppliers
            WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id AND id = NEW.supplier_id;
            SELECT * INTO settings_row FROM procurement.company_procurement_settings
            WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id AND id = NEW.company_procurement_settings_id;
            SELECT * INTO destination_row FROM operations.business_locations
            WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id
              AND branch_id = NEW.branch_id AND id = NEW.destination_location_id;
            SELECT * INTO policy_row FROM procurement.weight_processing_policies
            WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id AND id = NEW.weight_processing_policy_id;

            IF reservation_row.id IS NULL OR reservation_row.session_id <> NEW.id OR
               reservation_row.sequence <> NEW.cloud_reference_sequence OR
               reservation_row.rendered_reference <> NEW.cloud_reference OR
               reservation_row.policy_version <> NEW.reference_policy_version_snapshot OR
               supplier_row.id IS NULL OR supplier_row.status <> 1 OR
               supplier_row.version <> NEW.supplier_version_snapshot OR
               supplier_row.code <> NEW.supplier_code_snapshot OR
               supplier_row.name <> NEW.supplier_name_snapshot OR
               supplier_row.product_scope_mode <> NEW.supplier_product_scope_mode_snapshot OR
               settings_row.id IS NULL OR settings_row.version <> NEW.procurement_settings_version_snapshot OR
               settings_row.default_branch_id <> NEW.branch_id OR
               settings_row.vehicle_selection_mode <> NEW.vehicle_selection_mode_snapshot OR
               settings_row.default_destination_location_id <> NEW.destination_location_id OR
               settings_row.default_weight_processing_policy_id <> NEW.weight_processing_policy_id OR
               destination_row.id IS NULL OR destination_row.status <> 1 OR
               destination_row.version <> NEW.destination_location_version_snapshot OR
               destination_row.code <> NEW.destination_location_code_snapshot OR
               destination_row.name <> NEW.destination_location_name_snapshot OR
               policy_row.id IS NULL OR policy_row.status <> 1 OR
               policy_row.version <> NEW.weight_policy_version_snapshot OR
               policy_row.decimal_places <> NEW.weight_decimal_places_snapshot OR
               (CASE policy_row.processing_method WHEN 'Standard' THEN 0 WHEN 'Floor' THEN 1 WHEN 'Ceiling' THEN 2 ELSE -1 END) <> NEW.weight_processing_method_snapshot
            THEN
                RAISE EXCEPTION 'Commercial Receiving Session snapshot does not match validated master facts.' USING ERRCODE = '55000';
            END IF;

            IF NEW.receiving_vehicle_id IS NOT NULL THEN
                SELECT * INTO vehicle_row FROM procurement.receiving_vehicles
                WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id AND id = NEW.receiving_vehicle_id;
                IF vehicle_row.id IS NULL OR vehicle_row.status <> 1 OR
                   vehicle_row.version <> NEW.receiving_vehicle_version_snapshot OR
                   vehicle_row.code <> NEW.vehicle_code_snapshot OR
                   vehicle_row.registration_number <> NEW.vehicle_registration_snapshot OR
                   vehicle_row.display_name IS DISTINCT FROM NEW.vehicle_display_name_snapshot
                THEN
                    RAISE EXCEPTION 'Commercial Receiving Vehicle snapshot does not match validated master facts.' USING ERRCODE = '55000';
                END IF;
            END IF;
            RETURN NEW;
        END;
        $function$;
        CREATE TRIGGER tr_commercial_receiving_sessions_validate_snapshot
        BEFORE INSERT ON procurement.commercial_receiving_sessions
        FOR EACH ROW EXECUTE FUNCTION procurement.validate_commercial_receiving_session_snapshot();

        CREATE FUNCTION procurement.validate_commercial_receiving_entry_snapshot()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE session_row record;
        DECLARE product_row record;
        DECLARE scope_row record;
        DECLARE bag_row record;
        DECLARE standard_row record;
        BEGIN
            SELECT * INTO session_row FROM procurement.commercial_receiving_sessions
            WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id AND id = NEW.receiving_session_id;
            SELECT * INTO product_row FROM catalog.products
            WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id AND id = NEW.product_id;
            SELECT * INTO bag_row FROM procurement.bag_types
            WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id AND id = NEW.bag_type_id;
            IF session_row.id IS NULL OR product_row.id IS NULL OR product_row.status <> 1 OR NOT product_row.is_purchasable OR
               product_row.version <> NEW.product_version_snapshot OR product_row.code <> NEW.product_code_snapshot OR
               product_row.name <> NEW.product_name_snapshot OR product_row.product_type <> NEW.product_type_snapshot OR
               product_row.processing_family_code IS DISTINCT FROM NEW.processing_family_code_snapshot OR
               bag_row.id IS NULL OR bag_row.status <> 1 OR bag_row.version <> NEW.bag_type_version_snapshot OR
               bag_row.code <> NEW.bag_type_code_snapshot OR bag_row.name <> NEW.bag_type_name_snapshot OR
               bag_row.construction_class <> NEW.bag_construction_class_snapshot OR
               bag_row.standard_tare_weight_kg <> NEW.bag_tare_weight_kg_snapshot OR
               bag_row.is_returnable <> NEW.bag_returnable_snapshot OR
               session_row.supplier_product_scope_mode_snapshot <> NEW.supplier_scope_mode_snapshot OR
               session_row.weight_decimal_places_snapshot <> NEW.decimal_places_snapshot OR
               session_row.weight_processing_method_snapshot <> NEW.processing_method_snapshot
            THEN
                RAISE EXCEPTION 'Commercial Receiving Entry snapshot does not match validated master facts.' USING ERRCODE = '55000';
            END IF;

            IF NEW.supplier_scope_mode_snapshot = 2 THEN
                SELECT * INTO scope_row FROM procurement.supplier_product_scopes
                WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id
                  AND id = NEW.supplier_product_scope_id;
                IF scope_row.id IS NULL OR scope_row.status <> 1 OR
                   scope_row.version <> NEW.supplier_product_scope_version_snapshot OR
                   scope_row.supplier_id <> session_row.supplier_id OR scope_row.product_id <> NEW.product_id
                THEN
                    RAISE EXCEPTION 'Commercial Receiving Supplier scope snapshot is invalid.' USING ERRCODE = '55000';
                END IF;
            END IF;

            IF NEW.product_standard_bag_weight_id IS NOT NULL THEN
                SELECT * INTO standard_row FROM catalog.product_standard_bag_weights
                WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id
                  AND id = NEW.product_standard_bag_weight_id;
                IF standard_row.id IS NULL OR standard_row.status <> 1 OR
                   standard_row.version <> NEW.product_standard_bag_weight_version_snapshot OR
                   standard_row.product_id <> NEW.product_id OR standard_row.bag_type_id <> NEW.bag_type_id OR
                   standard_row.label IS DISTINCT FROM NEW.standard_bag_weight_label_snapshot OR
                   standard_row.standard_content_weight_kg <> NEW.standard_content_weight_kg_snapshot
                THEN
                    RAISE EXCEPTION 'Commercial Receiving standard Bag Weight snapshot is invalid.' USING ERRCODE = '55000';
                END IF;
            END IF;
            RETURN NEW;
        END;
        $function$;
        CREATE TRIGGER tr_commercial_receiving_entries_validate_snapshot
        BEFORE INSERT ON procurement.commercial_receiving_entries
        FOR EACH ROW EXECUTE FUNCTION procurement.validate_commercial_receiving_entry_snapshot();
        """;
}
