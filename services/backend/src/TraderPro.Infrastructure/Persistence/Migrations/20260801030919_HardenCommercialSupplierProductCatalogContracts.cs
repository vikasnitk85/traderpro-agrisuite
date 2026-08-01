using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenCommercialSupplierProductCatalogContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_suppliers_email",
                schema: "procurement",
                table: "suppliers");

            migrationBuilder.AddCheckConstraint(
                name: "ck_suppliers_email",
                schema: "procurement",
                table: "suppliers",
                sql: "email IS NULL OR (email = lower(btrim(email)) AND length(email) <= 254 AND email !~ '[[:space:][:cntrl:]]' AND length(email) - length(replace(email, '@', '')) = 1 AND position('@' in email) > 1 AND position('@' in email) < length(email))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_suppliers_email",
                schema: "procurement",
                table: "suppliers");

            migrationBuilder.AddCheckConstraint(
                name: "ck_suppliers_email",
                schema: "procurement",
                table: "suppliers",
                sql: "email IS NULL OR (email = lower(btrim(email)) AND position('@' in email) > 1)");
        }
    }
}
