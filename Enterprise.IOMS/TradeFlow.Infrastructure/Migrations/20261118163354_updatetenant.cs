using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updatetenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DropIndexIfPresent(migrationBuilder, "IX_Tenants_CustomDomain");
            DropIndexIfPresent(migrationBuilder, "IX_Tenants_Subdomain");
            DropColumnIfPresent(migrationBuilder, "CustomDomain");
            DropColumnIfPresent(migrationBuilder, "Subdomain");
            RenameColumnIfPresent(migrationBuilder, "LicenseDomainName", "ApplicationDomain");

            migrationBuilder.Sql(
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Tenants') AND name = N'IX_Tenants_ApplicationDomain') " +
                "CREATE UNIQUE INDEX [IX_Tenants_ApplicationDomain] ON [dbo].[Tenants] ([ApplicationDomain]) WHERE [ApplicationDomain] IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropIndexIfPresent(migrationBuilder, "IX_Tenants_ApplicationDomain");
            RenameColumnIfPresent(migrationBuilder, "ApplicationDomain", "LicenseDomainName");
            AddColumnIfMissing(migrationBuilder, "CustomDomain", "nvarchar(255) NULL");
            AddColumnIfMissing(migrationBuilder, "Subdomain", "nvarchar(63) NULL");
            CreateUniqueIndexIfMissing(migrationBuilder, "IX_Tenants_CustomDomain", "CustomDomain");
            CreateUniqueIndexIfMissing(migrationBuilder, "IX_Tenants_Subdomain", "Subdomain");
        }

        private static void DropIndexIfPresent(MigrationBuilder migrationBuilder, string name) =>
            migrationBuilder.Sql(
                $"IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Tenants') AND name = N'{name}') " +
                $"DROP INDEX [{name}] ON [dbo].[Tenants];");

        private static void DropColumnIfPresent(MigrationBuilder migrationBuilder, string name) =>
            migrationBuilder.Sql(
                $"IF COL_LENGTH(N'dbo.Tenants', N'{name}') IS NOT NULL " +
                $"ALTER TABLE [dbo].[Tenants] DROP COLUMN [{name}];");

        private static void AddColumnIfMissing(MigrationBuilder migrationBuilder, string name, string definition) =>
            migrationBuilder.Sql(
                $"IF COL_LENGTH(N'dbo.Tenants', N'{name}') IS NULL " +
                $"ALTER TABLE [dbo].[Tenants] ADD [{name}] {definition};");

        private static void RenameColumnIfPresent(MigrationBuilder migrationBuilder, string oldName, string newName) =>
            migrationBuilder.Sql(
                $"IF COL_LENGTH(N'dbo.Tenants', N'{oldName}') IS NOT NULL AND COL_LENGTH(N'dbo.Tenants', N'{newName}') IS NULL " +
                $"EXEC sp_rename N'[dbo].[Tenants].[{oldName}]', N'{newName}', N'COLUMN';");

        private static void CreateUniqueIndexIfMissing(MigrationBuilder migrationBuilder, string name, string column) =>
            migrationBuilder.Sql(
                $"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Tenants') AND name = N'{name}') " +
                $"CREATE UNIQUE INDEX [{name}] ON [dbo].[Tenants] ([{column}]) WHERE [{column}] IS NOT NULL;");
    }
}
