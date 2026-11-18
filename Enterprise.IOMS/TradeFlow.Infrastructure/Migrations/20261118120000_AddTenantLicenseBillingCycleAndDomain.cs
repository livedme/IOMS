using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TradeFlow.Infrastructure.Data;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261118120000_AddTenantLicenseBillingCycleAndDomain")]
    public partial class AddTenantLicenseBillingCycleAndDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddColumnIfMissing(migrationBuilder, "LicenseBillingCycle", "nvarchar(50) NULL");
            AddColumnIfMissing(migrationBuilder, "LicenseDomainName", "nvarchar(255) NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropColumnIfPresent(migrationBuilder, "LicenseBillingCycle");
            DropColumnIfPresent(migrationBuilder, "LicenseDomainName");
        }

        private static void AddColumnIfMissing(MigrationBuilder migrationBuilder, string name, string definition) =>
            migrationBuilder.Sql(
                $"IF COL_LENGTH(N'Tenants', N'{name}') IS NULL " +
                $"ALTER TABLE [Tenants] ADD [{name}] {definition};");

        private static void DropColumnIfPresent(MigrationBuilder migrationBuilder, string name) =>
            migrationBuilder.Sql(
                $"IF COL_LENGTH(N'Tenants', N'{name}') IS NOT NULL " +
                $"ALTER TABLE [Tenants] DROP COLUMN [{name}];");
    }
}
