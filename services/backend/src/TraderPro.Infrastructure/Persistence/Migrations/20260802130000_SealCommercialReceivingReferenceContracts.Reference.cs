namespace TraderPro.Infrastructure.Persistence.Migrations;

public partial class SealCommercialReceivingReferenceContracts
{
    private const string RejectDuplicateSessionReservationsSql =
        """
        DO $block$
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM procurement.commercial_receiving_reference_reservations
                GROUP BY workspace_id, company_id, session_id
                HAVING count(*) > 1) THEN
                RAISE EXCEPTION 'Task 7C1 cannot seal duplicate Receiving Session reference reservations; reconcile the database before retrying the migration.'
                    USING ERRCODE = '55000';
            END IF;
        END;
        $block$;
        """;

    private const string SealReferenceReservationSql =
        """
        CREATE OR REPLACE FUNCTION procurement.guard_commercial_receiving_reference_reservation()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE claim_row record;
        DECLARE existing_session_reservation uuid;
        BEGIN
            IF TG_OP = 'DELETE' THEN
                RAISE EXCEPTION 'Commercial Receiving reference reservations cannot be deleted.' USING ERRCODE = '55000';
            END IF;

            IF TG_OP = 'INSERT' THEN
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
                    'TraderPro.CommercialReceiving.Reference.v1' || E'\n' ||
                    NEW.workspace_id::text || E'\n' || NEW.company_id::text,
                    0));

                IF claim_row.operation_id IS NULL OR
                   claim_row.operation_type <> 'StartCommercialReceivingSession' OR
                   claim_row.session_id <> NEW.session_id OR
                   claim_row.request_hash <> NEW.request_hash THEN
                    RAISE EXCEPTION 'Commercial Receiving reference reservation does not match its Start claim.' USING ERRCODE = '55000';
                END IF;

                SELECT id INTO existing_session_reservation
                FROM procurement.commercial_receiving_reference_reservations
                WHERE workspace_id = NEW.workspace_id
                  AND company_id = NEW.company_id
                  AND session_id = NEW.session_id
                FOR SHARE;
                IF FOUND THEN
                    RAISE EXCEPTION 'A Receiving Session already owns a reference reservation.'
                        USING ERRCODE = '23505',
                              CONSTRAINT = 'ux_commercial_receiving_reference_reservations_session';
                END IF;
                RETURN NEW;
            END IF;

            IF NEW.id <> OLD.id OR
               NEW.workspace_id <> OLD.workspace_id OR
               NEW.company_id <> OLD.company_id OR
               NEW.policy_id <> OLD.policy_id OR
               NEW.period_key <> OLD.period_key OR
               NEW.operation_id <> OLD.operation_id OR
               NEW.session_id <> OLD.session_id OR
               NEW.request_hash <> OLD.request_hash OR
               NEW.policy_version <> OLD.policy_version OR
               NEW.sequence <> OLD.sequence OR
               NEW.rendered_reference <> OLD.rendered_reference OR
               NEW.reserved_at_utc <> OLD.reserved_at_utc OR
               NEW.created_at_utc <> OLD.created_at_utc OR
               OLD.consumed_at_utc IS NOT NULL OR
               NEW.consumed_at_utc IS NULL OR
               NEW.consumed_at_utc < NEW.reserved_at_utc THEN
                RAISE EXCEPTION 'Commercial Receiving reference reservation transition is invalid.' USING ERRCODE = '55000';
            END IF;
            RETURN NEW;
        END;
        $function$;
        """;
}
