namespace TraderPro.Infrastructure.Persistence.Migrations;

public partial class FinalizeCommercialReceivingBackendContracts
{
    private const string EnforceCommercialReceivingOwnershipStateMachineSql =
        """
        CREATE OR REPLACE FUNCTION procurement.guard_commercial_receiving_ownership_mutation()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE heartbeat_transition boolean := false;
        DECLARE acquisition_transition boolean := false;
        DECLARE transfer_transition boolean := false;
        DECLARE closure_transition boolean := false;
        BEGIN
            IF TG_OP = 'DELETE' THEN
                RAISE EXCEPTION 'Commercial Receiving ownership cannot be deleted.' USING ERRCODE = '55000';
            END IF;
            IF ROW(NEW.id, NEW.workspace_id, NEW.company_id,
                   NEW.receiving_session_id, NEW.created_at_utc)
               IS DISTINCT FROM
               ROW(OLD.id, OLD.workspace_id, OLD.company_id,
                   OLD.receiving_session_id, OLD.created_at_utc) OR
               NEW.version <> OLD.version + 1 OR
               NEW.updated_at_utc < OLD.updated_at_utc THEN
                RAISE EXCEPTION 'Commercial Receiving ownership revision is invalid.' USING ERRCODE = '55000';
            END IF;

            heartbeat_transition :=
                NEW.editor_device_id = OLD.editor_device_id AND
                NEW.ownership_generation = OLD.ownership_generation AND
                OLD.lease_id IS NOT NULL AND
                NEW.lease_id = OLD.lease_id AND
                NEW.last_heartbeat_at_utc > OLD.last_heartbeat_at_utc AND
                NEW.last_heartbeat_at_utc = NEW.updated_at_utc AND
                NEW.lease_expires_at_utc > OLD.lease_expires_at_utc AND
                NEW.lease_expires_at_utc > NEW.last_heartbeat_at_utc AND
                NEW.last_reacquired_at_utc IS NOT DISTINCT FROM OLD.last_reacquired_at_utc AND
                NEW.last_transferred_at_utc IS NOT DISTINCT FROM OLD.last_transferred_at_utc AND
                NEW.last_transferred_by_user_id IS NOT DISTINCT FROM OLD.last_transferred_by_user_id;

            acquisition_transition :=
                NEW.editor_device_id = OLD.editor_device_id AND
                NEW.ownership_generation = OLD.ownership_generation AND
                (OLD.lease_id IS NULL OR OLD.lease_expires_at_utc <= NEW.updated_at_utc) AND
                NEW.lease_id IS NOT NULL AND
                NEW.lease_id IS DISTINCT FROM OLD.lease_id AND
                substring(NEW.lease_id::text, 15, 1) = '7' AND
                NEW.last_heartbeat_at_utc = NEW.updated_at_utc AND
                NEW.last_reacquired_at_utc = NEW.updated_at_utc AND
                NEW.lease_expires_at_utc > NEW.updated_at_utc AND
                NEW.last_transferred_at_utc IS NOT DISTINCT FROM OLD.last_transferred_at_utc AND
                NEW.last_transferred_by_user_id IS NOT DISTINCT FROM OLD.last_transferred_by_user_id;

            transfer_transition :=
                NEW.editor_device_id <> OLD.editor_device_id AND
                NEW.ownership_generation = OLD.ownership_generation + 1 AND
                NEW.lease_id IS NULL AND
                NEW.lease_expires_at_utc IS NULL AND
                NEW.last_heartbeat_at_utc IS NULL AND
                NEW.last_reacquired_at_utc IS NOT DISTINCT FROM OLD.last_reacquired_at_utc AND
                NEW.last_transferred_at_utc = NEW.updated_at_utc AND
                NEW.last_transferred_at_utc >= OLD.updated_at_utc AND
                NEW.last_transferred_by_user_id IS NOT NULL;

            closure_transition :=
                NEW.editor_device_id = OLD.editor_device_id AND
                NEW.ownership_generation = OLD.ownership_generation AND
                OLD.lease_id IS NOT NULL AND
                NEW.lease_id IS NULL AND
                NEW.lease_expires_at_utc IS NULL AND
                NEW.last_heartbeat_at_utc IS NULL AND
                NEW.last_reacquired_at_utc IS NOT DISTINCT FROM OLD.last_reacquired_at_utc AND
                NEW.last_transferred_at_utc IS NOT DISTINCT FROM OLD.last_transferred_at_utc AND
                NEW.last_transferred_by_user_id IS NOT DISTINCT FROM OLD.last_transferred_by_user_id;

            IF COALESCE(heartbeat_transition, false)::integer +
               COALESCE(acquisition_transition, false)::integer +
               COALESCE(transfer_transition, false)::integer +
               COALESCE(closure_transition, false)::integer <> 1 THEN
                RAISE EXCEPTION 'Commercial Receiving ownership update is not a recognized state transition.' USING ERRCODE = '55000';
            END IF;
            RETURN NEW;
        END;
        $function$;

        CREATE OR REPLACE FUNCTION procurement.validate_commercial_receiving_ownership_state()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE session_row record;
        DECLARE target_device_id uuid;
        DECLARE target_credential_id uuid;
        BEGIN
            SELECT * INTO session_row
            FROM procurement.commercial_receiving_sessions
            WHERE workspace_id = NEW.workspace_id
              AND company_id = NEW.company_id
              AND id = NEW.receiving_session_id
            FOR SHARE;
            IF session_row.id IS NULL OR
               session_row.ownership_generation <> NEW.ownership_generation THEN
                RAISE EXCEPTION 'Commercial Receiving Session and ownership generations must agree.' USING ERRCODE = '55000';
            END IF;

            IF session_row.status = 2 THEN
                IF NEW.lease_id IS NOT NULL OR
                   NEW.lease_expires_at_utc IS NOT NULL OR
                   NEW.last_heartbeat_at_utc IS NOT NULL THEN
                    RAISE EXCEPTION 'Submitted Commercial Receiving ownership must be closed.' USING ERRCODE = '55000';
                END IF;
            ELSE
                SELECT device.id, credential.id
                INTO target_device_id, target_credential_id
                FROM platform.devices device
                JOIN platform.device_credentials credential
                  ON credential.workspace_id = device.workspace_id
                 AND credential.device_id = device.id
                 AND credential.revoked_at_utc IS NULL
                WHERE device.workspace_id = NEW.workspace_id
                  AND device.id = NEW.editor_device_id
                  AND device.status = 1
                FOR SHARE OF device, credential;
                IF target_device_id IS NULL OR target_credential_id IS NULL THEN
                    RAISE EXCEPTION 'Commercial Receiving editor must be an active credentialed Device.' USING ERRCODE = '55000';
                END IF;
                IF NEW.lease_id IS NULL AND
                   (NEW.ownership_generation = 1 OR
                    NEW.last_transferred_at_utc IS NULL OR
                    NEW.last_transferred_by_user_id IS NULL) THEN
                    RAISE EXCEPTION 'Missing Commercial Receiving lease requires a completed ownership transfer.' USING ERRCODE = '55000';
                END IF;
            END IF;
            RETURN NULL;
        END;
        $function$;

        CREATE OR REPLACE FUNCTION procurement.validate_commercial_receiving_ownership_transition_pair()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE session_row record;
        DECLARE ownership_row record;
        DECLARE changed_row jsonb := to_jsonb(NEW);
        DECLARE previous_row jsonb := CASE
            WHEN TG_OP = 'UPDATE' THEN to_jsonb(OLD) ELSE NULL END;
        DECLARE session_key uuid := CASE
            WHEN TG_TABLE_NAME = 'commercial_receiving_sessions'
                THEN (changed_row ->> 'id')::uuid
            ELSE (changed_row ->> 'receiving_session_id')::uuid END;
        BEGIN
            SELECT * INTO session_row
            FROM procurement.commercial_receiving_sessions
            WHERE id = session_key
            FOR SHARE;
            SELECT * INTO ownership_row
            FROM procurement.commercial_receiving_ownerships
            WHERE receiving_session_id = session_key
            FOR SHARE;
            IF session_row.id IS NULL OR ownership_row.id IS NULL OR
               session_row.workspace_id <> ownership_row.workspace_id OR
               session_row.company_id <> ownership_row.company_id OR
               session_row.ownership_generation <> ownership_row.ownership_generation THEN
                RAISE EXCEPTION 'Commercial Receiving ownership transition must advance its Session revision.' USING ERRCODE = '55000';
            END IF;
            IF session_row.status = 2 AND
               (ownership_row.lease_id IS NOT NULL OR
                ownership_row.lease_expires_at_utc IS NOT NULL OR
                ownership_row.last_heartbeat_at_utc IS NOT NULL) THEN
                RAISE EXCEPTION 'Commercial Receiving submission must close ownership in the same transaction.' USING ERRCODE = '55000';
            END IF;
            IF TG_TABLE_NAME = 'commercial_receiving_ownerships' AND TG_OP = 'UPDATE' AND
               (previous_row ->> 'ownership_generation')::bigint =
                   (changed_row ->> 'ownership_generation')::bigint AND
               previous_row ->> 'lease_id' IS NOT NULL AND
               changed_row ->> 'lease_id' IS NULL AND
               session_row.status <> 2 THEN
                RAISE EXCEPTION 'Commercial Receiving lease closure requires Session submission.' USING ERRCODE = '55000';
            END IF;
            RETURN NULL;
        END;
        $function$;
        """;
}
