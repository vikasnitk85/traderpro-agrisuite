namespace TraderPro.Infrastructure.Persistence.Migrations;

public partial class SealCommercialReceivingReferenceContracts
{
    private const string CanonicalSnapshotValidationSql =
        """
        CREATE OR REPLACE FUNCTION procurement.validate_commercial_receiving_session_snapshot()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE reservation_operation_id uuid;
        DECLARE claim_row record;
        DECLARE reservation_row record;
        DECLARE reference_policy_row record;
        DECLARE counter_row record;
        DECLARE supplier_row record;
        DECLARE settings_row record;
        DECLARE destination_row record;
        DECLARE weight_policy_row record;
        DECLARE vehicle_row record;
        BEGIN
            SELECT operation_id INTO reservation_operation_id
            FROM procurement.commercial_receiving_reference_reservations
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND id = NEW.reference_reservation_id;
            IF NOT FOUND THEN
                RAISE EXCEPTION 'Commercial Receiving Session requires its reserved reference.' USING ERRCODE = '55000';
            END IF;

            PERFORM pg_advisory_xact_lock(hashtextextended(
                'Procurement.CommercialReceiving.MobileSyncOperation' || E'\n' || reservation_operation_id::text,
                0));
            SELECT * INTO claim_row
            FROM sync.commercial_receiving_operation_claims
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND operation_id = reservation_operation_id
            FOR SHARE;
            PERFORM pg_advisory_xact_lock(hashtextextended(
                'TraderPro.CommercialReceiving.Reference.v1' || E'\n' ||
                NEW.workspace_id::text || E'\n' || NEW.company_id::text,
                0));
            SELECT * INTO reservation_row
            FROM procurement.commercial_receiving_reference_reservations
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND id = NEW.reference_reservation_id
            FOR SHARE;
            SELECT * INTO reference_policy_row
            FROM procurement.commercial_receiving_reference_policies
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND id = reservation_row.policy_id
            FOR SHARE;
            SELECT * INTO counter_row
            FROM procurement.commercial_receiving_reference_counters
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND policy_id = reservation_row.policy_id
              AND period_key = reservation_row.period_key
            FOR SHARE;
            PERFORM pg_advisory_xact_lock(hashtextextended(
                'TraderPro.CommercialReceiving.Session.v1' || E'\n' ||
                NEW.workspace_id::text || E'\n' || NEW.company_id::text || E'\n' || NEW.id::text,
                0));
            PERFORM procurement.lock_procurement_defaults(
                NEW.workspace_id,
                NEW.company_id);

            SELECT * INTO supplier_row
            FROM procurement.suppliers
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND id = NEW.supplier_id
            FOR SHARE;
            SELECT * INTO settings_row
            FROM procurement.company_procurement_settings
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND id = NEW.company_procurement_settings_id
            FOR SHARE;
            SELECT * INTO destination_row
            FROM operations.business_locations
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND branch_id = NEW.branch_id
              AND id = NEW.destination_location_id
            FOR SHARE;
            SELECT * INTO weight_policy_row
            FROM procurement.weight_processing_policies
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND id = NEW.weight_processing_policy_id
            FOR SHARE;
            IF NEW.receiving_vehicle_id IS NOT NULL THEN
                SELECT * INTO vehicle_row
                FROM procurement.receiving_vehicles
                WHERE workspace_id = NEW.workspace_id
                  AND company_id = NEW.company_id
                  AND id = NEW.receiving_vehicle_id
                FOR SHARE;
            END IF;

            IF claim_row.operation_id IS NULL OR
               claim_row.operation_type <> 'StartCommercialReceivingSession' OR
               claim_row.session_id <> NEW.id OR
               claim_row.request_hash <> reservation_row.request_hash OR
               reservation_row.session_id <> NEW.id OR
               reservation_row.operation_id <> reservation_operation_id OR
               reservation_row.policy_id IS NULL OR
               char_length(reservation_row.period_key) = 0 OR
               reservation_row.sequence <= 0 OR
               reservation_row.sequence <> NEW.cloud_reference_sequence OR
               reservation_row.rendered_reference <> NEW.cloud_reference OR
               reservation_row.policy_version <> NEW.reference_policy_version_snapshot OR
               reservation_row.consumed_at_utc < reservation_row.reserved_at_utc OR
               reference_policy_row.id IS NULL OR
               reference_policy_row.document_type <> 'CommercialReceiving' OR
               counter_row.policy_id IS NULL OR
               counter_row.next_number <= reservation_row.sequence OR
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
               weight_policy_row.id IS NULL OR weight_policy_row.status <> 1 OR
               weight_policy_row.version <> NEW.weight_policy_version_snapshot OR
               weight_policy_row.decimal_places <> NEW.weight_decimal_places_snapshot OR
               (CASE weight_policy_row.processing_method
                    WHEN 'Standard' THEN 0 WHEN 'Floor' THEN 1 WHEN 'Ceiling' THEN 2
                    ELSE -1 END) <> NEW.weight_processing_method_snapshot THEN
                RAISE EXCEPTION 'Commercial Receiving Session snapshot does not match locked reference and master facts.' USING ERRCODE = '55000';
            END IF;

            IF NEW.vehicle_selection_mode_snapshot = 2 THEN
                IF NEW.receiving_vehicle_id IS NOT NULL OR
                   NEW.receiving_vehicle_version_snapshot IS NOT NULL OR
                   NEW.vehicle_code_snapshot IS NOT NULL OR
                   NEW.vehicle_registration_snapshot IS NOT NULL OR
                   NEW.vehicle_display_name_snapshot IS NOT NULL THEN
                    RAISE EXCEPTION 'Disabled Commercial Receiving Vehicle selection cannot retain vehicle facts.' USING ERRCODE = '55000';
                END IF;
            ELSIF NEW.receiving_vehicle_id IS NULL THEN
                IF NEW.receiving_vehicle_version_snapshot IS NOT NULL OR
                   NEW.vehicle_code_snapshot IS NOT NULL OR
                   NEW.vehicle_registration_snapshot IS NOT NULL OR
                   NEW.vehicle_display_name_snapshot IS NOT NULL THEN
                    RAISE EXCEPTION 'Commercial Receiving Vehicle identity and snapshot must be supplied together.' USING ERRCODE = '55000';
                END IF;
            ELSIF vehicle_row.id IS NULL OR vehicle_row.status <> 1 OR
                  vehicle_row.version <> NEW.receiving_vehicle_version_snapshot OR
                  vehicle_row.code <> NEW.vehicle_code_snapshot OR
                  vehicle_row.registration_number <> NEW.vehicle_registration_snapshot OR
                  vehicle_row.display_name IS DISTINCT FROM NEW.vehicle_display_name_snapshot THEN
                RAISE EXCEPTION 'Commercial Receiving Vehicle snapshot does not match locked master facts.' USING ERRCODE = '55000';
            END IF;
            RETURN NEW;
        END;
        $function$;

        CREATE OR REPLACE FUNCTION procurement.validate_commercial_receiving_entry_snapshot()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE claim_row record;
        DECLARE session_row record;
        DECLARE ownership_row record;
        DECLARE product_row record;
        DECLARE scope_row record;
        DECLARE bag_row record;
        DECLARE standard_row record;
        BEGIN
            PERFORM pg_advisory_xact_lock(hashtextextended(
                'Procurement.CommercialReceiving.MobileSyncOperation' || E'\n' || NEW.operation_id::text,
                0));
            SELECT * INTO claim_row
            FROM sync.commercial_receiving_operation_claims
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND operation_id = NEW.operation_id
            FOR SHARE;
            PERFORM pg_advisory_xact_lock(hashtextextended(
                'TraderPro.CommercialReceiving.Session.v1' || E'\n' ||
                NEW.workspace_id::text || E'\n' || NEW.company_id::text || E'\n' || NEW.receiving_session_id::text,
                0));
            SELECT * INTO session_row
            FROM procurement.commercial_receiving_sessions
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND id = NEW.receiving_session_id
            FOR SHARE;
            SELECT * INTO ownership_row
            FROM procurement.commercial_receiving_ownerships
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND receiving_session_id = NEW.receiving_session_id
            FOR SHARE;
            SELECT * INTO product_row
            FROM catalog.products
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND id = NEW.product_id
            FOR SHARE;
            IF NEW.supplier_scope_mode_snapshot = 2 THEN
                SELECT * INTO scope_row
                FROM procurement.supplier_product_scopes
                WHERE workspace_id = NEW.workspace_id
                  AND company_id = NEW.company_id
                  AND id = NEW.supplier_product_scope_id
                FOR SHARE;
            END IF;
            SELECT * INTO bag_row
            FROM procurement.bag_types
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND id = NEW.bag_type_id
            FOR SHARE;
            IF NEW.product_standard_bag_weight_id IS NOT NULL THEN
                SELECT * INTO standard_row
                FROM catalog.product_standard_bag_weights
                WHERE workspace_id = NEW.workspace_id
                  AND company_id = NEW.company_id
                  AND id = NEW.product_standard_bag_weight_id
                FOR SHARE;
            END IF;

            IF claim_row.operation_id IS NULL OR
               claim_row.operation_type <> 'RecordCommercialReceivingEntry' OR
               claim_row.session_id <> NEW.receiving_session_id OR
               session_row.id IS NULL OR session_row.status <> 1 OR
               ownership_row.id IS NULL OR
               ownership_row.ownership_generation <> session_row.ownership_generation OR
               product_row.id IS NULL OR product_row.status <> 1 OR NOT product_row.is_purchasable OR
               product_row.version <> NEW.product_version_snapshot OR
               product_row.code <> NEW.product_code_snapshot OR
               product_row.name <> NEW.product_name_snapshot OR
               product_row.product_type <> NEW.product_type_snapshot OR
               product_row.processing_family_code IS DISTINCT FROM NEW.processing_family_code_snapshot OR
               bag_row.id IS NULL OR bag_row.status <> 1 OR
               bag_row.version <> NEW.bag_type_version_snapshot OR
               bag_row.code <> NEW.bag_type_code_snapshot OR
               bag_row.name <> NEW.bag_type_name_snapshot OR
               bag_row.construction_class <> NEW.bag_construction_class_snapshot OR
               bag_row.standard_tare_weight_kg <> NEW.bag_tare_weight_kg_snapshot OR
               bag_row.is_returnable <> NEW.bag_returnable_snapshot OR
               session_row.supplier_product_scope_mode_snapshot <> NEW.supplier_scope_mode_snapshot OR
               session_row.weight_decimal_places_snapshot <> NEW.decimal_places_snapshot OR
               session_row.weight_processing_method_snapshot <> NEW.processing_method_snapshot THEN
                RAISE EXCEPTION 'Commercial Receiving Entry snapshot does not match locked master facts.' USING ERRCODE = '55000';
            END IF;

            IF NEW.supplier_scope_mode_snapshot = 2 THEN
                IF scope_row.id IS NULL OR scope_row.status <> 1 OR
                   scope_row.version <> NEW.supplier_product_scope_version_snapshot OR
                   scope_row.supplier_id <> session_row.supplier_id OR
                   scope_row.product_id <> NEW.product_id THEN
                    RAISE EXCEPTION 'Commercial Receiving Supplier scope snapshot does not match locked master facts.' USING ERRCODE = '55000';
                END IF;
            END IF;

            IF NEW.product_standard_bag_weight_id IS NOT NULL THEN
                IF standard_row.id IS NULL OR standard_row.status <> 1 OR
                   standard_row.version <> NEW.product_standard_bag_weight_version_snapshot OR
                   standard_row.product_id <> NEW.product_id OR
                   standard_row.bag_type_id <> NEW.bag_type_id OR
                   standard_row.label IS DISTINCT FROM NEW.standard_bag_weight_label_snapshot OR
                   standard_row.standard_content_weight_kg <> NEW.standard_content_weight_kg_snapshot THEN
                    RAISE EXCEPTION 'Commercial Receiving standard Bag Weight snapshot does not match locked master facts.' USING ERRCODE = '55000';
                END IF;
            END IF;
            RETURN NEW;
        END;
        $function$;
        """;
}
