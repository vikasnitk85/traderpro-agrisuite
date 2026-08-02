namespace TraderPro.Infrastructure.Persistence.Migrations;

public partial class HardenCommercialReceivingBackendContracts
{
    private const string TransitionAndAudienceSql =
        """
        CREATE OR REPLACE FUNCTION procurement.guard_commercial_receiving_session_mutation()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        BEGIN
            IF TG_OP = 'DELETE' THEN
                RAISE EXCEPTION 'Commercial Receiving Sessions cannot be deleted.' USING ERRCODE = '55000';
            END IF;
            IF ROW(NEW.id, NEW.workspace_id, NEW.company_id, NEW.branch_id,
                   NEW.cloud_reference, NEW.cloud_reference_sequence,
                   NEW.reference_reservation_id, NEW.reference_policy_version_snapshot,
                   NEW.external_reference, NEW.supplier_id, NEW.supplier_version_snapshot,
                   NEW.supplier_code_snapshot, NEW.supplier_name_snapshot,
                   NEW.supplier_product_scope_mode_snapshot,
                   NEW.company_procurement_settings_id, NEW.procurement_settings_version_snapshot,
                   NEW.vehicle_selection_mode_snapshot, NEW.destination_location_id,
                   NEW.destination_location_version_snapshot, NEW.destination_location_code_snapshot,
                   NEW.destination_location_name_snapshot, NEW.weight_processing_policy_id,
                   NEW.weight_policy_version_snapshot, NEW.weight_decimal_places_snapshot,
                   NEW.weight_processing_method_snapshot, NEW.receiving_vehicle_id,
                   NEW.receiving_vehicle_version_snapshot, NEW.vehicle_code_snapshot,
                   NEW.vehicle_registration_snapshot, NEW.vehicle_display_name_snapshot,
                   NEW.started_at_device_utc, NEW.started_at_server_utc, NEW.created_at_utc)
               IS DISTINCT FROM
               ROW(OLD.id, OLD.workspace_id, OLD.company_id, OLD.branch_id,
                   OLD.cloud_reference, OLD.cloud_reference_sequence,
                   OLD.reference_reservation_id, OLD.reference_policy_version_snapshot,
                   OLD.external_reference, OLD.supplier_id, OLD.supplier_version_snapshot,
                   OLD.supplier_code_snapshot, OLD.supplier_name_snapshot,
                   OLD.supplier_product_scope_mode_snapshot,
                   OLD.company_procurement_settings_id, OLD.procurement_settings_version_snapshot,
                   OLD.vehicle_selection_mode_snapshot, OLD.destination_location_id,
                   OLD.destination_location_version_snapshot, OLD.destination_location_code_snapshot,
                   OLD.destination_location_name_snapshot, OLD.weight_processing_policy_id,
                   OLD.weight_policy_version_snapshot, OLD.weight_decimal_places_snapshot,
                   OLD.weight_processing_method_snapshot, OLD.receiving_vehicle_id,
                   OLD.receiving_vehicle_version_snapshot, OLD.vehicle_code_snapshot,
                   OLD.vehicle_registration_snapshot, OLD.vehicle_display_name_snapshot,
                   OLD.started_at_device_utc, OLD.started_at_server_utc, OLD.created_at_utc)
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
                 NEW.submitted_at_utc IS NULL AND
                 NEW.ownership_generation = OLD.ownership_generation) OR
                (NEW.next_expected_local_sequence = OLD.next_expected_local_sequence AND
                 NEW.entry_count = OLD.entry_count AND
                 NEW.processed_total_weight_kg = OLD.processed_total_weight_kg AND
                 NEW.submitted_at_utc IS NULL AND
                 NEW.ownership_generation = OLD.ownership_generation + 1))
            THEN
                RAISE EXCEPTION 'Commercial Receiving in-progress aggregate transition is invalid.' USING ERRCODE = '55000';
            END IF;
            IF OLD.status = 1 AND NEW.status = 2 AND NOT (
                NEW.next_expected_local_sequence = OLD.next_expected_local_sequence + 1 AND
                NEW.entry_count = OLD.entry_count AND
                NEW.processed_total_weight_kg = OLD.processed_total_weight_kg AND
                NEW.submitted_at_utc IS NOT NULL AND
                NEW.ownership_generation = OLD.ownership_generation)
            THEN
                RAISE EXCEPTION 'Commercial Receiving submission transition is invalid.' USING ERRCODE = '55000';
            END IF;
            RETURN NEW;
        END;
        $function$;

        CREATE OR REPLACE FUNCTION procurement.guard_commercial_receiving_ownership_mutation()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        BEGIN
            IF TG_OP = 'DELETE' THEN
                RAISE EXCEPTION 'Commercial Receiving ownership cannot be deleted.' USING ERRCODE = '55000';
            END IF;
            IF ROW(NEW.id, NEW.workspace_id, NEW.company_id, NEW.receiving_session_id, NEW.created_at_utc)
               IS DISTINCT FROM ROW(OLD.id, OLD.workspace_id, OLD.company_id, OLD.receiving_session_id, OLD.created_at_utc) OR
               NEW.version <> OLD.version + 1 OR NEW.updated_at_utc < OLD.updated_at_utc OR
               NEW.ownership_generation NOT IN (OLD.ownership_generation, OLD.ownership_generation + 1)
            THEN
                RAISE EXCEPTION 'Commercial Receiving ownership transition is invalid.' USING ERRCODE = '55000';
            END IF;

            IF NEW.ownership_generation = OLD.ownership_generation + 1 THEN
                IF NEW.editor_device_id = OLD.editor_device_id OR
                   NEW.lease_id IS NOT NULL OR NEW.lease_expires_at_utc IS NOT NULL OR
                   NEW.last_heartbeat_at_utc IS NOT NULL OR
                   NEW.last_transferred_at_utc IS NULL OR
                   NEW.last_transferred_at_utc <> NEW.updated_at_utc OR
                   NEW.last_transferred_at_utc < OLD.updated_at_utc OR
                   NEW.last_transferred_by_user_id IS NULL OR
                   NEW.last_reacquired_at_utc IS DISTINCT FROM OLD.last_reacquired_at_utc
                THEN
                    RAISE EXCEPTION 'Commercial Receiving transfer transition is invalid.' USING ERRCODE = '55000';
                END IF;
            ELSE
                IF NEW.editor_device_id <> OLD.editor_device_id OR
                   ROW(NEW.last_transferred_at_utc, NEW.last_transferred_by_user_id)
                     IS DISTINCT FROM ROW(OLD.last_transferred_at_utc, OLD.last_transferred_by_user_id)
                THEN
                    RAISE EXCEPTION 'Commercial Receiving renewal transition cannot alter transfer facts.' USING ERRCODE = '55000';
                END IF;
                IF NEW.lease_id IS DISTINCT FROM OLD.lease_id AND NEW.lease_id IS NOT NULL AND NOT (
                    NEW.last_reacquired_at_utc = NEW.updated_at_utc AND
                    (OLD.lease_id IS NULL OR
                     (OLD.lease_expires_at_utc <= NEW.updated_at_utc AND
                      NEW.last_reacquired_at_utc IS DISTINCT FROM OLD.last_reacquired_at_utc)))
                THEN
                    RAISE EXCEPTION 'A valid Commercial Receiving lease cannot be rotated.' USING ERRCODE = '55000';
                END IF;
            END IF;
            RETURN NEW;
        END;
        $function$;

        CREATE OR REPLACE FUNCTION procurement.validate_commercial_receiving_ownership_state()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE session_row record;
        BEGIN
            SELECT * INTO session_row FROM procurement.commercial_receiving_sessions
            WHERE workspace_id = NEW.workspace_id AND company_id = NEW.company_id
              AND id = NEW.receiving_session_id;
            IF session_row.id IS NULL OR session_row.ownership_generation <> NEW.ownership_generation THEN
                RAISE EXCEPTION 'Commercial Receiving Session and ownership generations must agree.' USING ERRCODE = '55000';
            END IF;
            IF session_row.status = 2 THEN
                IF NEW.lease_id IS NOT NULL THEN
                    RAISE EXCEPTION 'Submitted Commercial Receiving ownership must be closed.' USING ERRCODE = '55000';
                END IF;
            ELSE
                IF NOT EXISTS (
                    SELECT 1 FROM platform.devices d
                    JOIN platform.device_credentials c
                      ON c.workspace_id = d.workspace_id AND c.device_id = d.id
                     AND c.revoked_at_utc IS NULL
                    WHERE d.workspace_id = NEW.workspace_id
                      AND d.id = NEW.editor_device_id AND d.status = 1)
                THEN
                    RAISE EXCEPTION 'Commercial Receiving editor must be an active credentialed Device.' USING ERRCODE = '55000';
                END IF;
                IF NEW.lease_id IS NULL AND (NEW.ownership_generation = 1 OR NEW.last_transferred_at_utc IS NULL) THEN
                    RAISE EXCEPTION 'Missing Commercial Receiving lease requires a completed ownership transfer.' USING ERRCODE = '55000';
                END IF;
            END IF;
            RETURN NULL;
        END;
        $function$;

        CREATE FUNCTION procurement.validate_commercial_receiving_ownership_transition_pair()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE session_row record;
        DECLARE ownership_row record;
        DECLARE changed_row jsonb := to_jsonb(NEW);
        DECLARE session_key uuid := CASE WHEN TG_TABLE_NAME = 'commercial_receiving_sessions'
            THEN (changed_row ->> 'id')::uuid ELSE (changed_row ->> 'receiving_session_id')::uuid END;
        BEGIN
            SELECT * INTO session_row FROM procurement.commercial_receiving_sessions WHERE id = session_key;
            SELECT * INTO ownership_row FROM procurement.commercial_receiving_ownerships WHERE receiving_session_id = session_key;
            IF session_row.id IS NULL OR ownership_row.id IS NULL OR
               session_row.workspace_id <> ownership_row.workspace_id OR
               session_row.company_id <> ownership_row.company_id OR
               session_row.ownership_generation <> ownership_row.ownership_generation
            THEN
                RAISE EXCEPTION 'Commercial Receiving ownership transition must advance its Session revision.' USING ERRCODE = '55000';
            END IF;
            RETURN NULL;
        END;
        $function$;
        CREATE CONSTRAINT TRIGGER tr_commercial_receiving_sessions_ownership_pair
        AFTER INSERT OR UPDATE ON procurement.commercial_receiving_sessions
        DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
        EXECUTE FUNCTION procurement.validate_commercial_receiving_ownership_transition_pair();
        CREATE CONSTRAINT TRIGGER tr_commercial_receiving_ownerships_session_pair
        AFTER INSERT OR UPDATE ON procurement.commercial_receiving_ownerships
        DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
        EXECUTE FUNCTION procurement.validate_commercial_receiving_ownership_transition_pair();

        CREATE FUNCTION platform.validate_commercial_outbox_audience_row()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE message_workspace uuid;
        DECLARE message_stream smallint;
        BEGIN
            SELECT workspace_id, event_stream INTO message_workspace, message_stream
            FROM platform.outbox_messages WHERE id = NEW.outbox_message_id;
            IF message_stream IS DISTINCT FROM 3 OR message_workspace IS DISTINCT FROM NEW.workspace_id THEN
                RAISE EXCEPTION 'Commercial audience requires a same-Workspace CommercialMobileSync message.' USING ERRCODE = '55000';
            END IF;
            IF (NEW.audience = 1 AND NEW.target_device_id IS NOT NULL) OR
               (NEW.audience = 2 AND NEW.target_device_id IS NULL) THEN
                RAISE EXCEPTION 'Commercial audience target shape is invalid.' USING ERRCODE = '55000';
            END IF;
            RETURN NEW;
        END;
        $function$;
        CREATE TRIGGER tr_commercial_outbox_audiences_validate
        BEFORE INSERT ON platform.commercial_outbox_audiences
        FOR EACH ROW EXECUTE FUNCTION platform.validate_commercial_outbox_audience_row();

        CREATE FUNCTION platform.validate_commercial_outbox_audience_completeness()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE changed_row jsonb := COALESCE(to_jsonb(NEW), to_jsonb(OLD));
        DECLARE message_key uuid := CASE WHEN TG_TABLE_NAME = 'outbox_messages'
            THEN (changed_row ->> 'id')::uuid ELSE (changed_row ->> 'outbox_message_id')::uuid END;
        DECLARE message_stream smallint;
        DECLARE audience_count bigint;
        BEGIN
            SELECT event_stream INTO message_stream FROM platform.outbox_messages WHERE id = message_key;
            SELECT count(*) INTO audience_count FROM platform.commercial_outbox_audiences WHERE outbox_message_id = message_key;
            IF message_stream = 3 AND audience_count <> 1 THEN
                RAISE EXCEPTION 'Every CommercialMobileSync message requires exactly one commercial audience.' USING ERRCODE = '55000';
            END IF;
            IF message_stream IS DISTINCT FROM 3 AND audience_count <> 0 THEN
                RAISE EXCEPTION 'Internal and POC MobileSync messages cannot have commercial audiences.' USING ERRCODE = '55000';
            END IF;
            RETURN NULL;
        END;
        $function$;
        CREATE CONSTRAINT TRIGGER tr_outbox_messages_commercial_audience
        AFTER INSERT OR UPDATE OR DELETE ON platform.outbox_messages
        DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
        EXECUTE FUNCTION platform.validate_commercial_outbox_audience_completeness();
        CREATE CONSTRAINT TRIGGER tr_commercial_outbox_audiences_complete
        AFTER INSERT OR UPDATE OR DELETE ON platform.commercial_outbox_audiences
        DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
        EXECUTE FUNCTION platform.validate_commercial_outbox_audience_completeness();
        """;
}
