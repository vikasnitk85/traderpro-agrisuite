using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenTwoDeviceProcurementPocContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_receiving_session_pocs_lease_shape",
                schema: "procurement",
                table: "receiving_session_pocs");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receiving_session_pocs_state_shape",
                schema: "procurement",
                table: "receiving_session_pocs",
                sql: "(status = 1\n    AND lease_id IS NOT NULL\n    AND lease_expires_at_utc IS NOT NULL\n    AND last_lease_heartbeat_at_utc IS NOT NULL\n    AND submitted_at_utc IS NULL\n    AND approved_at_utc IS NULL\n    AND approved_by_device_id IS NULL)\nOR (status = 2\n    AND lease_id IS NULL\n    AND lease_expires_at_utc IS NULL\n    AND last_lease_heartbeat_at_utc IS NULL\n    AND submitted_at_utc IS NOT NULL\n    AND approved_at_utc IS NULL\n    AND approved_by_device_id IS NULL)\nOR (status IN (3, 4)\n    AND lease_id IS NULL\n    AND lease_expires_at_utc IS NULL\n    AND last_lease_heartbeat_at_utc IS NULL\n    AND submitted_at_utc IS NOT NULL\n    AND approved_at_utc IS NOT NULL\n    AND approved_by_device_id IS NOT NULL)");

            migrationBuilder.Sql(
                """
                DROP TRIGGER tr_receiving_session_pocs_workspace_immutable
                    ON procurement.receiving_session_pocs;
                DROP FUNCTION
                    procurement.reject_receiving_session_poc_workspace_change();

                CREATE FUNCTION
                    procurement.reject_receiving_session_poc_identity_or_delete()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION
                            'Receiving Session POC rows cannot be deleted.'
                            USING ERRCODE = '55000';
                    END IF;

                    IF NEW.workspace_id IS DISTINCT FROM OLD.workspace_id
                        OR NEW.editor_device_id IS DISTINCT FROM
                            OLD.editor_device_id THEN
                        RAISE EXCEPTION
                            'Receiving Session POC workspace and editor are immutable.'
                            USING ERRCODE = '55000';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER
                    tr_receiving_session_pocs_identity_immutable
                BEFORE UPDATE OF workspace_id, editor_device_id
                ON procurement.receiving_session_pocs
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.reject_receiving_session_poc_identity_or_delete();

                CREATE TRIGGER
                    tr_receiving_session_pocs_delete_immutable
                BEFORE DELETE
                ON procurement.receiving_session_pocs
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.reject_receiving_session_poc_identity_or_delete();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER
                    tr_receiving_session_pocs_delete_immutable
                    ON procurement.receiving_session_pocs;
                DROP TRIGGER
                    tr_receiving_session_pocs_identity_immutable
                    ON procurement.receiving_session_pocs;
                DROP FUNCTION
                    procurement.reject_receiving_session_poc_identity_or_delete();

                CREATE FUNCTION
                    procurement.reject_receiving_session_poc_workspace_change()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF NEW.workspace_id IS DISTINCT FROM OLD.workspace_id THEN
                        RAISE EXCEPTION
                            'Receiving Session POC workspace ownership is immutable.'
                            USING ERRCODE = '55000';
                    END IF;

                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER
                    tr_receiving_session_pocs_workspace_immutable
                BEFORE UPDATE OF workspace_id
                ON procurement.receiving_session_pocs
                FOR EACH ROW
                EXECUTE FUNCTION
                    procurement.reject_receiving_session_poc_workspace_change();
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_receiving_session_pocs_state_shape",
                schema: "procurement",
                table: "receiving_session_pocs");

            migrationBuilder.AddCheckConstraint(
                name: "ck_receiving_session_pocs_lease_shape",
                schema: "procurement",
                table: "receiving_session_pocs",
                sql: "(status = 1\n    AND lease_id IS NOT NULL\n    AND lease_expires_at_utc IS NOT NULL\n    AND last_lease_heartbeat_at_utc IS NOT NULL)\nOR (status <> 1\n    AND lease_id IS NULL\n    AND lease_expires_at_utc IS NULL\n    AND last_lease_heartbeat_at_utc IS NULL)");
        }
    }
}
