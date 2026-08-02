namespace TraderPro.Infrastructure.Persistence.Migrations;

public partial class HardenCommercialReceivingBackendContracts
{
    private const string MasterPayloadFunctionsSql =
        """
        CREATE FUNCTION sync.commercial_master_decimal_text(value_text text)
        RETURNS text LANGUAGE sql IMMUTABLE STRICT AS $function$
            SELECT to_char(value_text::numeric, 'FM99999999999990.000000');
        $function$;

        CREATE FUNCTION sync.build_commercial_master_payload(kind text, source jsonb)
        RETURNS jsonb LANGUAGE plpgsql IMMUTABLE AS $function$
        DECLARE status_text text := CASE COALESCE(source ->> 'status', '1') WHEN '1' THEN 'Active' ELSE 'Inactive' END;
        DECLARE common jsonb := jsonb_build_object(
            'contractVersion', 1,
            'id', source -> 'id',
            'version', (source ->> 'version')::bigint,
            'status', status_text);
        BEGIN
            RETURN common || CASE kind
                WHEN 'CompanyProcurementSettings' THEN jsonb_build_object(
                    'defaultBranchId', source -> 'default_branch_id',
                    'defaultDestinationLocationId', source -> 'default_destination_location_id',
                    'defaultWeightProcessingPolicyId', source -> 'default_weight_processing_policy_id',
                    'vehicleSelectionMode', CASE source ->> 'vehicle_selection_mode' WHEN '1' THEN 'Optional' WHEN '2' THEN 'Disabled' END)
                WHEN 'BusinessLocation' THEN jsonb_build_object(
                    'branchId', source -> 'branch_id', 'code', source ->> 'code',
                    'name', source ->> 'name', 'localName', source -> 'local_name',
                    'locationType', CASE source ->> 'location_type'
                        WHEN '1' THEN 'Warehouse' WHEN '2' THEN 'Yard' WHEN '3' THEN 'Mill'
                        WHEN '4' THEN 'Office' WHEN '5' THEN 'Other' END)
                WHEN 'ReceivingVehicle' THEN jsonb_build_object(
                    'code', source ->> 'code', 'registrationNumber', source ->> 'registration_number',
                    'displayName', source -> 'display_name',
                    'vehicleType', CASE source ->> 'vehicle_type'
                        WHEN '1' THEN 'Truck' WHEN '2' THEN 'Tractor' WHEN '3' THEN 'Van' WHEN '4' THEN 'Other' END)
                WHEN 'BagType' THEN jsonb_build_object(
                    'code', source ->> 'code', 'name', source ->> 'name',
                    'localName', source -> 'local_name',
                    'constructionClass', CASE source ->> 'construction_class'
                        WHEN '1' THEN 'Jute' WHEN '2' THEN 'SinglePlastic'
                        WHEN '3' THEN 'DoublePlastic' WHEN '4' THEN 'Other' END,
                    'standardTareWeightKg', sync.commercial_master_decimal_text(source ->> 'standard_tare_weight_kg'),
                    'isReturnable', source -> 'is_returnable')
                WHEN 'WeightProcessingPolicy' THEN jsonb_build_object(
                    'code', source ->> 'code', 'name', source ->> 'name',
                    'decimalPlaces', (source ->> 'decimal_places')::integer,
                    'processingMethod', source ->> 'processing_method')
                WHEN 'Supplier' THEN jsonb_build_object(
                    'code', source ->> 'code', 'name', source ->> 'name',
                    'localName', source -> 'local_name',
                    'supplierType', CASE source ->> 'supplier_type' WHEN '1' THEN 'Individual' WHEN '2' THEN 'Business' END,
                    'productScopeMode', CASE source ->> 'product_scope_mode' WHEN '1' THEN 'Unrestricted' WHEN '2' THEN 'Restricted' END)
                WHEN 'SupplierProductScope' THEN jsonb_build_object(
                    'supplierId', source -> 'supplier_id', 'productId', source -> 'product_id')
                WHEN 'ProductGroup' THEN jsonb_build_object(
                    'code', source ->> 'code', 'name', source ->> 'name', 'localName', source -> 'local_name')
                WHEN 'Product' THEN jsonb_build_object(
                    'productGroupId', source -> 'product_group_id', 'code', source ->> 'code',
                    'name', source ->> 'name', 'localName', source -> 'local_name',
                    'productType', CASE source ->> 'product_type'
                        WHEN '1' THEN 'RawMaterial' WHEN '2' THEN 'FinishedGood'
                        WHEN '3' THEN 'ByProduct' WHEN '4' THEN 'Consumable' WHEN '5' THEN 'Other' END,
                    'isPurchasable', source -> 'is_purchasable',
                    'processingFamilyCode', source -> 'processing_family_code')
                WHEN 'ProductStandardBagWeight' THEN jsonb_build_object(
                    'productId', source -> 'product_id', 'bagTypeId', source -> 'bag_type_id',
                    'label', source -> 'label',
                    'standardContentWeightKg', sync.commercial_master_decimal_text(source ->> 'standard_content_weight_kg'),
                    'isDefault', source -> 'is_default')
                ELSE NULL
            END;
        END;
        $function$;

        DROP TRIGGER tr_commercial_master_changes_immutable ON sync.commercial_master_changes;
        UPDATE sync.commercial_master_changes
        SET payload_json = sync.build_commercial_master_payload(master_type, payload_json);
        CREATE TRIGGER tr_commercial_master_changes_immutable
        BEFORE UPDATE OR DELETE ON sync.commercial_master_changes
        FOR EACH ROW EXECUTE FUNCTION sync.reject_commercial_master_change_mutation();

        CREATE OR REPLACE FUNCTION sync.capture_commercial_master_change()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE
            source jsonb := to_jsonb(NEW);
            kind text;
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
            PERFORM pg_advisory_xact_lock(hashtextextended(
                'TraderPro.CommercialMasterSync.CommitOrder.v1' || E'\n' || workspace_value::text || E'\n' || company_value::text, 0));
            INSERT INTO sync.commercial_master_changes(
                workspace_id, company_id, master_type, master_id,
                master_version, status, payload_json, occurred_at_utc)
            VALUES (workspace_value, company_value, kind,
                    (source ->> 'id')::uuid, (source ->> 'version')::bigint,
                    status_text, sync.build_commercial_master_payload(kind, source),
                    (source ->> 'updated_at_utc')::timestamptz)
            ON CONFLICT (workspace_id, company_id, master_type, master_id, master_version)
            DO NOTHING;
            RETURN NEW;
        END;
        $function$;
        """;

    private const string DropIntegrityFunctionsSql =
        """
        DROP TRIGGER IF EXISTS tr_commercial_outbox_audiences_complete ON platform.commercial_outbox_audiences;
        DROP TRIGGER IF EXISTS tr_outbox_messages_commercial_audience ON platform.outbox_messages;
        DROP TRIGGER IF EXISTS tr_commercial_outbox_audiences_validate ON platform.commercial_outbox_audiences;
        DROP FUNCTION IF EXISTS platform.validate_commercial_outbox_audience_completeness();
        DROP FUNCTION IF EXISTS platform.validate_commercial_outbox_audience_row();
        DROP TRIGGER IF EXISTS tr_commercial_receiving_ownerships_session_pair ON procurement.commercial_receiving_ownerships;
        DROP TRIGGER IF EXISTS tr_commercial_receiving_sessions_ownership_pair ON procurement.commercial_receiving_sessions;
        DROP FUNCTION IF EXISTS procurement.validate_commercial_receiving_ownership_transition_pair();
        DROP TRIGGER IF EXISTS tr_commercial_receiving_entries_validate_snapshot ON procurement.commercial_receiving_entries;
        DROP TRIGGER IF EXISTS tr_commercial_receiving_sessions_validate_snapshot ON procurement.commercial_receiving_sessions;
        DROP FUNCTION IF EXISTS procurement.validate_commercial_receiving_entry_snapshot();
        DROP FUNCTION IF EXISTS procurement.validate_commercial_receiving_session_snapshot();
        DROP TRIGGER IF EXISTS tr_commercial_receiving_reservations_consumption ON procurement.commercial_receiving_reference_reservations;
        DROP TRIGGER IF EXISTS tr_commercial_receiving_sessions_reservation ON procurement.commercial_receiving_sessions;
        DROP FUNCTION IF EXISTS procurement.validate_commercial_receiving_reservation_consumption();
        DROP TRIGGER IF EXISTS tr_commercial_receiving_reference_reservations_guard ON procurement.commercial_receiving_reference_reservations;
        DROP FUNCTION IF EXISTS procurement.guard_commercial_receiving_reference_reservation();
        DROP TRIGGER IF EXISTS tr_commercial_receiving_operation_claims_guard ON sync.commercial_receiving_operation_claims;
        DROP FUNCTION IF EXISTS sync.guard_commercial_receiving_operation_claim();
        DROP FUNCTION IF EXISTS sync.commercial_master_decimal_text(text);
        DROP FUNCTION IF EXISTS sync.build_commercial_master_payload(text, jsonb);
        """;

    private const string RestoreLegacyMasterCaptureSql =
        """
        CREATE OR REPLACE FUNCTION sync.capture_commercial_master_change()
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
            status_text := CASE COALESCE(source ->> 'status', '1')
                WHEN '1' THEN 'Active' ELSE 'Inactive' END;
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
                'TraderPro.CommercialMasterSync.CommitOrder.v1' || E'\n' ||
                workspace_value::text || E'\n' || company_value::text, 0));
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
        """;
}
