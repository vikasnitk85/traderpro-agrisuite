namespace TraderPro.Infrastructure.Persistence.Migrations;

public partial class FinalizeCommercialReceivingBackendContracts
{
    private const string SerializeCommercialReceivingClaimsSql =
        """
        CREATE OR REPLACE FUNCTION sync.guard_commercial_receiving_operation_claim()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        BEGIN
            IF TG_OP = 'DELETE' THEN
                RAISE EXCEPTION 'Commercial Receiving Operation claims cannot be deleted.' USING ERRCODE = '55000';
            END IF;
            IF TG_OP = 'INSERT' THEN
                IF NEW.state <> 1 OR NEW.attention_code IS NOT NULL OR
                   NEW.attention_message IS NOT NULL OR NEW.retryable OR
                   NEW.completed_at_utc IS NOT NULL THEN
                    RAISE EXCEPTION 'Commercial Receiving Operation claims begin Pending.' USING ERRCODE = '55000';
                END IF;
                RETURN NEW;
            END IF;

            IF ROW(NEW.workspace_id, NEW.company_id, NEW.command_scope,
                   NEW.operation_id, NEW.operation_type, NEW.session_id,
                   NEW.device_id, NEW.ownership_generation, NEW.request_hash,
                   NEW.first_seen_at_utc, NEW.created_at_utc)
               IS DISTINCT FROM
               ROW(OLD.workspace_id, OLD.company_id, OLD.command_scope,
                   OLD.operation_id, OLD.operation_type, OLD.session_id,
                   OLD.device_id, OLD.ownership_generation, OLD.request_hash,
                   OLD.first_seen_at_utc, OLD.created_at_utc) THEN
                RAISE EXCEPTION 'Commercial Receiving Operation claim identity is immutable.' USING ERRCODE = '55000';
            END IF;
            IF OLD.state IN (3, 4) THEN
                RAISE EXCEPTION 'Terminal Commercial Receiving Operation claims are immutable.' USING ERRCODE = '55000';
            END IF;
            IF NEW.updated_at_utc < OLD.updated_at_utc THEN
                RAISE EXCEPTION 'Commercial Receiving Operation claim time cannot regress.' USING ERRCODE = '55000';
            END IF;

            IF OLD.state = 1 AND NEW.state = 2 AND
               NEW.attention_code IS NOT NULL AND NEW.attention_message IS NOT NULL AND
               NEW.completed_at_utc IS NULL THEN
                RETURN NEW;
            END IF;
            IF OLD.state = 1 AND NEW.state = 3 AND
               NEW.attention_code IS NOT NULL AND NEW.attention_message IS NOT NULL AND
               NOT NEW.retryable AND NEW.completed_at_utc IS NULL THEN
                RETURN NEW;
            END IF;
            IF OLD.state = 2 AND NEW.state = 1 AND
               NEW.attention_code IS NULL AND NEW.attention_message IS NULL AND
               NOT NEW.retryable AND NEW.completed_at_utc IS NULL THEN
                RETURN NEW;
            END IF;
            IF OLD.state IN (1, 2) AND NEW.state = 4 AND
               NEW.attention_code IS NULL AND NEW.attention_message IS NULL AND
               NOT NEW.retryable AND NEW.completed_at_utc IS NOT NULL AND
               NEW.updated_at_utc = NEW.completed_at_utc AND
               NEW.completed_at_utc >= OLD.updated_at_utc THEN
                RETURN NEW;
            END IF;

            RAISE EXCEPTION 'Commercial Receiving Operation claim transition is invalid.' USING ERRCODE = '55000';
        END;
        $function$;

        CREATE TRIGGER tr_commercial_receiving_operation_claims_guard
        BEFORE INSERT OR UPDATE OR DELETE
        ON sync.commercial_receiving_operation_claims
        FOR EACH ROW EXECUTE FUNCTION sync.guard_commercial_receiving_operation_claim();

        CREATE OR REPLACE FUNCTION sync.validate_commercial_receiving_claim_completion()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        BEGIN
            IF NEW.state = 4 AND NOT EXISTS (
                SELECT 1
                FROM platform.idempotency_records completion
                WHERE completion.workspace_id = NEW.workspace_id
                  AND completion.command_type = NEW.command_scope
                  AND completion.idempotency_key = NEW.operation_id::text
                  AND completion.request_hash = NEW.request_hash
                  AND completion.status = 2
                  AND completion.result_payload_json IS NOT NULL
                  AND completion.result_status_code BETWEEN 200 AND 299
                  AND completion.completed_at_utc IS NOT NULL) THEN
                RAISE EXCEPTION 'Completed Commercial Receiving claim requires matching idempotency completion facts.' USING ERRCODE = '55000';
            END IF;
            RETURN NULL;
        END;
        $function$;
        DROP TRIGGER IF EXISTS tr_commercial_receiving_operation_claims_completion
            ON sync.commercial_receiving_operation_claims;
        CREATE CONSTRAINT TRIGGER tr_commercial_receiving_operation_claims_completion
        AFTER INSERT OR UPDATE
        ON sync.commercial_receiving_operation_claims
        DEFERRABLE INITIALLY DEFERRED
        FOR EACH ROW EXECUTE FUNCTION sync.validate_commercial_receiving_claim_completion();
        """;
}
