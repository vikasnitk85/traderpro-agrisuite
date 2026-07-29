using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenCloudCommandAndEventCursorSpike : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_workspace_id_sequence",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.AddColumn<short>(
                name: "event_stream",
                schema: "platform",
                table: "outbox_messages",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_workspace_id_event_stream_sequence",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "workspace_id", "event_stream", "sequence" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_outbox_messages_event_stream",
                schema: "platform",
                table: "outbox_messages",
                sql: "event_stream IN (1, 2)");

            migrationBuilder.Sql(
                """
                UPDATE platform.idempotency_records
                SET status = 3,
                    result_status_code = NULL
                WHERE status = 2
                  AND (
                      result_payload_json IS NOT NULL
                      AND jsonb_typeof(result_payload_json) = 'object'
                      AND jsonb_typeof(result_payload_json -> 'result') = 'object'
                      AND jsonb_typeof(result_payload_json -> 'correlationId') = 'string'
                      AND result_payload_json ->> 'correlationId'
                          ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
                      AND jsonb_typeof(result_payload_json -> 'result' -> 'id') = 'string'
                      AND result_payload_json -> 'result' ->> 'id'
                          ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
                      AND jsonb_typeof(result_payload_json -> 'result' -> 'name') = 'string'
                      AND btrim(result_payload_json -> 'result' ->> 'name') <> ''
                      AND jsonb_typeof(result_payload_json -> 'result' -> 'counter') = 'number'
                      AND result_payload_json -> 'result' ->> 'counter'
                          ~ '^[0-9]+$'
                      AND (result_payload_json -> 'result' ->> 'counter')::numeric
                          <= 9223372036854775807
                      AND jsonb_typeof(result_payload_json -> 'result' -> 'version') = 'number'
                      AND result_payload_json -> 'result' ->> 'version'
                          ~ '^[1-9][0-9]*$'
                      AND (result_payload_json -> 'result' ->> 'version')::numeric
                          <= 9223372036854775807
                  ) IS NOT TRUE;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION platform.reject_outbox_event_fact_update()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF ROW(
                        NEW.sequence,
                        NEW.workspace_id,
                        NEW.event_stream,
                        NEW.event_type,
                        NEW.event_version,
                        NEW.aggregate_type,
                        NEW.aggregate_id,
                        NEW.aggregate_version,
                        NEW.payload_json,
                        NEW.correlation_id,
                        NEW.occurred_at_utc)
                    IS DISTINCT FROM ROW(
                        OLD.sequence,
                        OLD.workspace_id,
                        OLD.event_stream,
                        OLD.event_type,
                        OLD.event_version,
                        OLD.aggregate_type,
                        OLD.aggregate_id,
                        OLD.aggregate_version,
                        OLD.payload_json,
                        OLD.correlation_id,
                        OLD.occurred_at_utc)
                    THEN
                        RAISE EXCEPTION
                            'Outbox event facts are immutable after insert.'
                            USING ERRCODE = '55000';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER tr_outbox_messages_immutable_event_facts
                BEFORE UPDATE ON platform.outbox_messages
                FOR EACH ROW
                EXECUTE FUNCTION platform.reject_outbox_event_fact_update();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS tr_outbox_messages_immutable_event_facts
                    ON platform.outbox_messages;
                DROP FUNCTION IF EXISTS platform.reject_outbox_event_fact_update();
                """);

            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_workspace_id_event_stream_sequence",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_outbox_messages_event_stream",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "event_stream",
                schema: "platform",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_workspace_id_sequence",
                schema: "platform",
                table: "outbox_messages",
                columns: new[] { "workspace_id", "sequence" });
        }
    }
}
