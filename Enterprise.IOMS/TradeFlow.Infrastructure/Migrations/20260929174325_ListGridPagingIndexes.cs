using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ListGridPagingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocktakes_TenantId",
                table: "Stocktakes");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturns_TenantId",
                table: "SalesReturns");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturns_TenantId",
                table: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_Kits_TenantId",
                table: "Kits");

            migrationBuilder.CreateIndex(
                name: "IX_Stocktakes_TenantId_IsDeleted_StartDate",
                table: "Stocktakes",
                columns: new[] { "TenantId", "IsDeleted", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Stocktakes_TenantId_IsDeleted_Status",
                table: "Stocktakes",
                columns: new[] { "TenantId", "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_IsDeleted_MovementDate",
                table: "StockMovements",
                columns: new[] { "TenantId", "IsDeleted", "MovementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_TenantId_IsDeleted_CreatedAt",
                table: "SalesReturns",
                columns: new[] { "TenantId", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_TenantId_IsDeleted_QuoteDate",
                table: "SalesQuotes",
                columns: new[] { "TenantId", "IsDeleted", "QuoteDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TenantId_IsDeleted_OrderDate",
                table: "SalesOrders",
                columns: new[] { "TenantId", "IsDeleted", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_TenantId_IsDeleted_CreatedAt",
                table: "PurchaseReturns",
                columns: new[] { "TenantId", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_IsDeleted_PurchaseDate",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "IsDeleted", "PurchaseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Kits_TenantId_IsDeleted",
                table: "Kits",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_TenantId_IsDeleted_WarehouseId",
                table: "Inventories",
                columns: new[] { "TenantId", "IsDeleted", "WarehouseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocktakes_TenantId_IsDeleted_StartDate",
                table: "Stocktakes");

            migrationBuilder.DropIndex(
                name: "IX_Stocktakes_TenantId_IsDeleted_Status",
                table: "Stocktakes");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_IsDeleted_MovementDate",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturns_TenantId_IsDeleted_CreatedAt",
                table: "SalesReturns");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotes_TenantId_IsDeleted_QuoteDate",
                table: "SalesQuotes");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_TenantId_IsDeleted_OrderDate",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseReturns_TenantId_IsDeleted_CreatedAt",
                table: "PurchaseReturns");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenantId_IsDeleted_PurchaseDate",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Kits_TenantId_IsDeleted",
                table: "Kits");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_TenantId_IsDeleted_WarehouseId",
                table: "Inventories");

            migrationBuilder.CreateIndex(
                name: "IX_Stocktakes_TenantId",
                table: "Stocktakes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_TenantId",
                table: "SalesReturns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_TenantId",
                table: "PurchaseReturns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Kits_TenantId",
                table: "Kits",
                column: "TenantId");
        }
    }
}
