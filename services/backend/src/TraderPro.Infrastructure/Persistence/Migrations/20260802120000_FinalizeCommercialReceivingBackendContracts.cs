using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeCommercialReceivingBackendContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReconcileLegacyCommercialReceivingSql);

            migrationBuilder.DropCheckConstraint(
                name: "ck_commercial_receiving_sessions_snapshots",
                schema: "procurement",
                table: "commercial_receiving_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_commercial_receiving_operation_claims_shape",
                schema: "sync",
                table: "commercial_receiving_operation_claims");

            migrationBuilder.AddCheckConstraint(
                name: "ck_commercial_receiving_sessions_snapshots",
                schema: "procurement",
                table: "commercial_receiving_sessions",
                sql: "supplier_version_snapshot > 0 AND supplier_product_scope_mode_snapshot IN (1, 2) AND procurement_settings_version_snapshot > 0 AND vehicle_selection_mode_snapshot IN (1, 2) AND destination_location_version_snapshot > 0 AND weight_policy_version_snapshot > 0 AND weight_decimal_places_snapshot BETWEEN 1 AND 3 AND weight_processing_method_snapshot IN (0, 1, 2) AND ((vehicle_selection_mode_snapshot = 2 AND receiving_vehicle_id IS NULL AND receiving_vehicle_version_snapshot IS NULL AND vehicle_code_snapshot IS NULL AND vehicle_registration_snapshot IS NULL AND vehicle_display_name_snapshot IS NULL) OR (vehicle_selection_mode_snapshot = 1 AND ((receiving_vehicle_id IS NULL AND receiving_vehicle_version_snapshot IS NULL AND vehicle_code_snapshot IS NULL AND vehicle_registration_snapshot IS NULL AND vehicle_display_name_snapshot IS NULL) OR (receiving_vehicle_id IS NOT NULL AND receiving_vehicle_version_snapshot > 0 AND vehicle_code_snapshot IS NOT NULL AND vehicle_registration_snapshot IS NOT NULL))))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_commercial_receiving_reference_reservations_uuidv7",
                schema: "procurement",
                table: "commercial_receiving_reference_reservations",
                sql: "substring(id::text, 15, 1) = '7' AND substring(operation_id::text, 15, 1) = '7' AND substring(session_id::text, 15, 1) = '7'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_commercial_receiving_operation_claims_shape",
                schema: "sync",
                table: "commercial_receiving_operation_claims",
                sql: "char_length(request_hash) = 64 AND request_hash ~ '^[0-9a-f]{64}$' AND (ownership_generation IS NULL OR ownership_generation > 0) AND ((state IN (1, 4) AND attention_code IS NULL AND attention_message IS NULL) OR (state IN (2, 3) AND attention_code IS NOT NULL AND attention_message IS NOT NULL)) AND ((state = 4 AND completed_at_utc IS NOT NULL) OR (state <> 4 AND completed_at_utc IS NULL)) AND (state NOT IN (3, 4) OR retryable = false)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_commercial_receiving_operation_claims_uuidv7",
                schema: "sync",
                table: "commercial_receiving_operation_claims",
                sql: "substring(operation_id::text, 15, 1) = '7' AND substring(session_id::text, 15, 1) = '7'");

            migrationBuilder.Sql(LockCommercialReceivingSnapshotsSql);
            migrationBuilder.Sql(EnforceCommercialReceivingOwnershipStateMachineSql);
            migrationBuilder.Sql(SerializeCommercialReceivingClaimsSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Task 7C1 schema rollback is not supported. Roll back the application and restore the database from backup.");
        }
    }
}
