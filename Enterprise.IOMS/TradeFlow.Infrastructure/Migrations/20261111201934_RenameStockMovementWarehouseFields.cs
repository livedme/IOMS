using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameStockMovementWarehouseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Warehouses_WarehouseId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_WarehouseId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_TenantId_ProductId_WarehouseId",
                table: "Inventories");

            // Preserve the previous single-warehouse association as the movement's source
            // before the old column is dropped.
            migrationBuilder.Sql(
                "UPDATE [StockMovements] SET [SourceWarehouseId] = [WarehouseId] WHERE [SourceWarehouseId] IS NULL;");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "StockMovements");

            migrationBuilder.RenameColumn(
                name: "DestinationWarehouseId",
                table: "StockMovements",
                newName: "TargetWarehouseId");

            migrationBuilder.AddColumn<Guid>(
                name: "TargetBranchId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                table: "Inventories",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "Inventories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SourceWarehouseId",
                table: "StockMovements",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TargetBranchId",
                table: "StockMovements",
                column: "TargetBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TargetWarehouseId",
                table: "StockMovements",
                column: "TargetWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_BranchId",
                table: "Inventories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_TenantId_ProductId_WarehouseId",
                table: "Inventories",
                columns: new[] { "TenantId", "ProductId", "WarehouseId" },
                unique: true,
                filter: "[WarehouseId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Branches_BranchId",
                table: "Inventories",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Branches_TargetBranchId",
                table: "StockMovements",
                column: "TargetBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Warehouses_SourceWarehouseId",
                table: "StockMovements",
                column: "SourceWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Warehouses_TargetWarehouseId",
                table: "StockMovements",
                column: "TargetWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Branches_BranchId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Branches_TargetBranchId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Warehouses_SourceWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Warehouses_TargetWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_SourceWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TargetBranchId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TargetWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_BranchId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_TenantId_ProductId_WarehouseId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "TargetBranchId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Inventories");

            migrationBuilder.RenameColumn(
                name: "TargetWarehouseId",
                table: "StockMovements",
                newName: "DestinationWarehouseId");

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                table: "Inventories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_WarehouseId",
                table: "StockMovements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_TenantId_ProductId_WarehouseId",
                table: "Inventories",
                columns: new[] { "TenantId", "ProductId", "WarehouseId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Warehouses_WarehouseId",
                table: "StockMovements",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
