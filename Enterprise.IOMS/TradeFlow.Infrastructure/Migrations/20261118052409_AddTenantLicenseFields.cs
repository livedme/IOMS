using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantLicenseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddColumnIfMissing(migrationBuilder, "LicenseKey", "nvarchar(256) NULL");
            AddColumnIfMissing(migrationBuilder, "LicenseId", "uniqueidentifier NULL");
            AddColumnIfMissing(migrationBuilder, "SubscriptionId", "uniqueidentifier NULL");
            AddColumnIfMissing(migrationBuilder, "SubscriptionNumber", "nvarchar(100) NULL");
            AddColumnIfMissing(migrationBuilder, "SubscriptionName", "nvarchar(200) NULL");
            AddColumnIfMissing(migrationBuilder, "LicensePlan", "nvarchar(50) NULL");
            AddColumnIfMissing(migrationBuilder, "LicenseStatus", "nvarchar(50) NULL");
            AddColumnIfMissing(migrationBuilder, "LicenseProjectId", "uniqueidentifier NULL");
            AddColumnIfMissing(migrationBuilder, "LicenseProjectName", "nvarchar(200) NULL");
            AddColumnIfMissing(migrationBuilder, "LicenseActivatedAt", "datetime2 NULL");
            AddColumnIfMissing(migrationBuilder, "LicenseValidFrom", "datetime2 NULL");
            AddColumnIfMissing(migrationBuilder, "LicenseValidUntil", "datetime2 NULL");
            AddColumnIfMissing(migrationBuilder, "LicenseCheckedAtUtc", "datetime2 NULL");
            AddColumnIfMissing(migrationBuilder, "LicenseAmount",
                "decimal(18,2) NOT NULL CONSTRAINT [DF_Tenants_LicenseAmount] DEFAULT ((0))");
            AddColumnIfMissing(migrationBuilder, "LicenseGracePeriod",
                "int NOT NULL CONSTRAINT [DF_Tenants_LicenseGracePeriod] DEFAULT ((0))");

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "LicenseActivatedAt", "LicenseAmount", "LicenseCheckedAtUtc", "LicenseGracePeriod", "LicenseId", "LicenseKey", "LicensePlan", "LicenseProjectId", "LicenseProjectName", "LicenseStatus", "LicenseValidFrom", "LicenseValidUntil", "SubscriptionId", "SubscriptionName", "SubscriptionNumber" },
                values: new object[] { null, 0m, null, 0, null, null, null, null, null, null, null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropColumnIfPresent(migrationBuilder, "LicenseKey");
            DropColumnIfPresent(migrationBuilder, "LicenseId");
            DropColumnIfPresent(migrationBuilder, "SubscriptionId");
            DropColumnIfPresent(migrationBuilder, "SubscriptionNumber");
            DropColumnIfPresent(migrationBuilder, "SubscriptionName");
            DropColumnIfPresent(migrationBuilder, "LicensePlan");
            DropColumnIfPresent(migrationBuilder, "LicenseStatus");
            DropColumnIfPresent(migrationBuilder, "LicenseProjectId");
            DropColumnIfPresent(migrationBuilder, "LicenseProjectName");
            DropColumnIfPresent(migrationBuilder, "LicenseActivatedAt");
            DropColumnIfPresent(migrationBuilder, "LicenseValidFrom");
            DropColumnIfPresent(migrationBuilder, "LicenseValidUntil");
            DropColumnIfPresent(migrationBuilder, "LicenseCheckedAtUtc");
            DropColumnIfPresent(migrationBuilder, "LicenseAmount");
            DropColumnIfPresent(migrationBuilder, "LicenseGracePeriod");
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
