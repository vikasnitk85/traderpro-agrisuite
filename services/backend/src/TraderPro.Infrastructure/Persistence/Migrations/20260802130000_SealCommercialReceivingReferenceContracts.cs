using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SealCommercialReceivingReferenceContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RejectDuplicateSessionReservationsSql);

            migrationBuilder.CreateIndex(
                name: "ux_commercial_receiving_reference_reservations_session",
                schema: "procurement",
                table: "commercial_receiving_reference_reservations",
                columns: new[] { "workspace_id", "company_id", "session_id" },
                unique: true);

            migrationBuilder.Sql(SealReferenceReservationSql);
            migrationBuilder.Sql(CanonicalSnapshotValidationSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Task 7C1 schema rollback is not supported. Roll back the application and restore the database from backup.");
        }
    }
}
