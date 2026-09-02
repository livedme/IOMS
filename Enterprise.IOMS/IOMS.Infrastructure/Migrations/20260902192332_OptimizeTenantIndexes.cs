using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeTenantIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockMovements_MovementDate",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_OrderDate",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_Status",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductSerials_Status",
                table: "ProductSerials");

            migrationBuilder.DropIndex(
                name: "IX_Products_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_EntryDate",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_DueDate",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_ProductId_WarehouseId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerName",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_MovementDate",
                table: "StockMovements",
                columns: new[] { "TenantId", "MovementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ProductId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TenantId_OrderDate",
                table: "SalesOrders",
                columns: new[] { "TenantId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TenantId_Status",
                table: "SalesOrders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_Status",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSerials_TenantId_ProductId",
                table: "ProductSerials",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSerials_TenantId_Status",
                table: "ProductSerials",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Name",
                table: "Products",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_EntryDate",
                table: "JournalEntries",
                columns: new[] { "TenantId", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_DueDate",
                table: "Invoices",
                columns: new[] { "TenantId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductId",
                table: "Inventories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_TenantId_ProductId_WarehouseId",
                table: "Inventories",
                columns: new[] { "TenantId", "ProductId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_CustomerName",
                table: "Customers",
                columns: new[] { "TenantId", "CustomerName" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId_NormalizedEmail",
                table: "AspNetUsers",
                columns: new[] { "TenantId", "NormalizedEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId_NormalizedUserName",
                table: "AspNetUsers",
                columns: new[] { "TenantId", "NormalizedUserName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_MovementDate",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ProductId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_TenantId_OrderDate",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_TenantId_Status",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenantId_Status",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductSerials_TenantId_ProductId",
                table: "ProductSerials");

            migrationBuilder.DropIndex(
                name: "IX_ProductSerials_TenantId_Status",
                table: "ProductSerials");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_TenantId_EntryDate",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_DueDate",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_ProductId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_TenantId_ProductId_WarehouseId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_CustomerName",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId_NormalizedEmail",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId_NormalizedUserName",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementDate",
                table: "StockMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_OrderDate",
                table: "SalesOrders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Status",
                table: "SalesOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSerials_Status",
                table: "ProductSerials",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_EntryDate",
                table: "JournalEntries",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DueDate",
                table: "Invoices",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductId_WarehouseId",
                table: "Inventories",
                columns: new[] { "ProductId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerName",
                table: "Customers",
                column: "CustomerName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");
        }
    }
}
