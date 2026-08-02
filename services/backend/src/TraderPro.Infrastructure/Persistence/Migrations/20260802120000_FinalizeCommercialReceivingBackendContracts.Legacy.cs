namespace TraderPro.Infrastructure.Persistence.Migrations;

public partial class FinalizeCommercialReceivingBackendContracts
{
    private const string ReconcileLegacyCommercialReceivingSql =
        """
        ALTER TABLE procurement.commercial_receiving_sessions
            ENABLE TRIGGER USER;
        DROP TRIGGER IF EXISTS tr_commercial_receiving_reference_reservations_guard
            ON procurement.commercial_receiving_reference_reservations;
        DROP TRIGGER IF EXISTS tr_commercial_receiving_operation_claims_guard
            ON sync.commercial_receiving_operation_claims;
        ALTER TABLE procurement.commercial_receiving_reference_reservations
            DROP CONSTRAINT IF EXISTS fk_commercial_receiving_reference_reservations_claim;

        CREATE TEMPORARY TABLE tmp_commercial_receiving_legacy_starts
            ON COMMIT DROP AS
        SELECT DISTINCT ON (s.workspace_id, s.company_id, s.id)
               s.workspace_id,
               s.company_id,
               s.id AS session_id,
               i.idempotency_key::uuid AS operation_id,
               i.request_hash,
               a.actor_device_id AS device_id,
               NULL::bigint AS ownership_generation,
               i.created_at_utc AS first_seen_at_utc,
               i.completed_at_utc
        FROM procurement.commercial_receiving_sessions s
        JOIN sync.commercial_receiving_operation_claims placeholder
          ON placeholder.workspace_id = s.workspace_id
         AND placeholder.company_id = s.company_id
         AND placeholder.operation_id = s.id
         AND placeholder.operation_type = 'LegacyCompletedCommercialReceivingSession'
        JOIN platform.idempotency_records i
          ON i.workspace_id = s.workspace_id
         AND i.command_type = 'Procurement.CommercialReceiving.MobileSyncOperation'
         AND i.status = 2
         AND i.completed_at_utc IS NOT NULL
         AND i.result_payload_json IS NOT NULL
         AND i.result_status_code BETWEEN 200 AND 299
         AND i.idempotency_key ~ '^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$'
         AND i.request_hash ~ '^[0-9a-f]{64}$'
         AND i.result_payload_json #>> '{result,cloudReference}' = s.cloud_reference
         AND (i.result_payload_json #>> '{result,sessionVersion}')::bigint = 1
        JOIN platform.audit_events a
          ON a.workspace_id = s.workspace_id
         AND a.company_id = s.company_id
         AND a.aggregate_id = s.id
         AND a.action = 'Procurement.CommercialReceiving.SessionStarted'
         AND a.correlation_id = i.result_payload_json ->> 'correlationId'
         AND a.actor_device_id IS NOT NULL
         AND (a.after_snapshot_json ->> 'sessionId')::uuid = s.id
         AND (a.after_snapshot_json ->> 'ownershipGeneration')::bigint > 0
        ORDER BY s.workspace_id, s.company_id, s.id, i.completed_at_utc;

        UPDATE procurement.commercial_receiving_reference_reservations r
        SET operation_id = reconstructed.operation_id,
            request_hash = reconstructed.request_hash
        FROM tmp_commercial_receiving_legacy_starts reconstructed
        WHERE r.workspace_id = reconstructed.workspace_id
          AND r.company_id = reconstructed.company_id
          AND r.session_id = reconstructed.session_id
          AND r.operation_id = reconstructed.session_id
          AND r.request_hash = repeat('0', 64);

        UPDATE sync.commercial_receiving_operation_claims claim
        SET operation_id = reconstructed.operation_id,
            operation_type = 'StartCommercialReceivingSession',
            device_id = reconstructed.device_id,
            ownership_generation = reconstructed.ownership_generation,
            request_hash = reconstructed.request_hash,
            first_seen_at_utc = reconstructed.first_seen_at_utc,
            completed_at_utc = reconstructed.completed_at_utc,
            created_at_utc = reconstructed.first_seen_at_utc,
            updated_at_utc = reconstructed.completed_at_utc
        FROM tmp_commercial_receiving_legacy_starts reconstructed
        WHERE claim.workspace_id = reconstructed.workspace_id
          AND claim.company_id = reconstructed.company_id
          AND claim.operation_id = reconstructed.session_id
          AND claim.operation_type = 'LegacyCompletedCommercialReceivingSession';

        INSERT INTO sync.commercial_receiving_operation_claims(
            workspace_id, company_id, command_scope, operation_id, operation_type,
            session_id, device_id, ownership_generation, request_hash, state,
            attention_code, attention_message, retryable, first_seen_at_utc,
            completed_at_utc, created_at_utc, updated_at_utc)
        SELECT e.workspace_id,
               e.company_id,
               'Procurement.CommercialReceiving.MobileSyncOperation',
               e.operation_id,
               'RecordCommercialReceivingEntry',
               e.receiving_session_id,
               a.actor_device_id,
               ownership.ownership_generation,
               i.request_hash,
               4,
               NULL,
               NULL,
               false,
               i.created_at_utc,
               i.completed_at_utc,
               i.created_at_utc,
               i.completed_at_utc
        FROM procurement.commercial_receiving_entries e
        JOIN procurement.commercial_receiving_ownerships ownership
          ON ownership.workspace_id = e.workspace_id
         AND ownership.company_id = e.company_id
         AND ownership.receiving_session_id = e.receiving_session_id
         AND ownership.ownership_generation = 1
         AND ownership.last_transferred_at_utc IS NULL
         AND ownership.last_transferred_by_user_id IS NULL
        JOIN platform.idempotency_records i
          ON i.workspace_id = e.workspace_id
         AND i.command_type = 'Procurement.CommercialReceiving.MobileSyncOperation'
         AND i.idempotency_key = e.operation_id::text
         AND i.status = 2
         AND i.completed_at_utc IS NOT NULL
         AND i.result_payload_json IS NOT NULL
         AND i.result_status_code BETWEEN 200 AND 299
         AND i.request_hash ~ '^[0-9a-f]{64}$'
        JOIN platform.audit_events a
          ON a.workspace_id = e.workspace_id
         AND a.company_id = e.company_id
         AND a.aggregate_id = e.receiving_session_id
         AND a.action = 'Procurement.CommercialReceiving.EntryAccepted'
         AND a.correlation_id = i.result_payload_json ->> 'correlationId'
         AND a.actor_device_id = ownership.editor_device_id
         AND (a.after_snapshot_json ->> 'entryId')::uuid = e.id
         AND (a.after_snapshot_json ->> 'localSequence')::bigint = e.local_sequence
        ON CONFLICT DO NOTHING;

        INSERT INTO sync.commercial_receiving_operation_claims(
            workspace_id, company_id, command_scope, operation_id, operation_type,
            session_id, device_id, ownership_generation, request_hash, state,
            attention_code, attention_message, retryable, first_seen_at_utc,
            completed_at_utc, created_at_utc, updated_at_utc)
        SELECT e.workspace_id,
               e.company_id,
               'Procurement.CommercialReceiving.MobileSyncOperation',
               e.operation_id,
               'RecordCommercialReceivingEntry',
               e.receiving_session_id,
               COALESCE(a.actor_device_id, ownership.editor_device_id),
               ownership.ownership_generation,
               COALESCE(i.request_hash, repeat('0', 64)),
               3,
               'COMMERCIAL_MOBILE_OPERATION_LEGACY_NON_REPLAYABLE',
               'The legacy operation identity cannot be reconstructed safely and cannot be replayed.',
               false,
               COALESCE(i.created_at_utc, e.accepted_at_server_utc),
               NULL,
               COALESCE(i.created_at_utc, e.accepted_at_server_utc),
               COALESCE(i.completed_at_utc, e.accepted_at_server_utc)
        FROM procurement.commercial_receiving_entries e
        JOIN procurement.commercial_receiving_ownerships ownership
          ON ownership.workspace_id = e.workspace_id
         AND ownership.company_id = e.company_id
         AND ownership.receiving_session_id = e.receiving_session_id
        LEFT JOIN platform.idempotency_records i
          ON i.workspace_id = e.workspace_id
         AND i.command_type = 'Procurement.CommercialReceiving.MobileSyncOperation'
         AND i.idempotency_key = e.operation_id::text
         AND i.request_hash ~ '^[0-9a-f]{64}$'
        LEFT JOIN LATERAL (
            SELECT audit.actor_device_id
            FROM platform.audit_events audit
            WHERE audit.workspace_id = e.workspace_id
              AND audit.company_id = e.company_id
              AND audit.aggregate_id = e.receiving_session_id
              AND audit.action = 'Procurement.CommercialReceiving.EntryAccepted'
              AND (audit.after_snapshot_json ->> 'entryId')::uuid = e.id
            ORDER BY audit.occurred_at_utc
            LIMIT 1) a ON true
        WHERE NOT EXISTS (
            SELECT 1
            FROM sync.commercial_receiving_operation_claims existing
            WHERE existing.workspace_id = e.workspace_id
              AND existing.company_id = e.company_id
              AND existing.operation_id = e.operation_id)
        ON CONFLICT DO NOTHING;

        UPDATE sync.commercial_receiving_operation_claims claim
        SET operation_type = 'LegacyNonReplayableCommercialReceivingSession',
            state = 3,
            attention_code = 'COMMERCIAL_MOBILE_OPERATION_LEGACY_NON_REPLAYABLE',
            attention_message = 'The legacy Start operation identity cannot be reconstructed safely and cannot be replayed.',
            retryable = false,
            completed_at_utc = NULL,
            updated_at_utc = GREATEST(claim.updated_at_utc, clock_timestamp())
        WHERE claim.operation_type = 'LegacyCompletedCommercialReceivingSession';

        SET CONSTRAINTS ALL IMMEDIATE;

        ALTER TABLE procurement.commercial_receiving_reference_reservations
            ADD CONSTRAINT fk_commercial_receiving_reference_reservations_claim
            FOREIGN KEY (workspace_id, company_id, operation_id)
            REFERENCES sync.commercial_receiving_operation_claims
                (workspace_id, company_id, operation_id)
            ON DELETE RESTRICT;

        CREATE OR REPLACE FUNCTION procurement.guard_commercial_receiving_reference_reservation()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE claim_row record;
        BEGIN
            IF TG_OP = 'DELETE' THEN
                RAISE EXCEPTION 'Commercial Receiving reference reservations cannot be deleted.' USING ERRCODE = '55000';
            END IF;
            IF TG_OP = 'INSERT' THEN
                SELECT * INTO claim_row
                FROM sync.commercial_receiving_operation_claims
                WHERE workspace_id = NEW.workspace_id
                  AND company_id = NEW.company_id
                  AND operation_id = NEW.operation_id
                FOR SHARE;
                IF NOT FOUND OR claim_row.operation_type <> 'StartCommercialReceivingSession' OR
                   claim_row.session_id <> NEW.session_id OR
                   claim_row.request_hash <> NEW.request_hash THEN
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
               NEW.consumed_at_utc < NEW.reserved_at_utc THEN
                RAISE EXCEPTION 'Commercial Receiving reference reservation transition is invalid.' USING ERRCODE = '55000';
            END IF;
            RETURN NEW;
        END;
        $function$;
        CREATE TRIGGER tr_commercial_receiving_reference_reservations_guard
        BEFORE INSERT OR UPDATE OR DELETE
        ON procurement.commercial_receiving_reference_reservations
        FOR EACH ROW EXECUTE FUNCTION procurement.guard_commercial_receiving_reference_reservation();

        SET CONSTRAINTS ALL DEFERRED;
        """;
}
