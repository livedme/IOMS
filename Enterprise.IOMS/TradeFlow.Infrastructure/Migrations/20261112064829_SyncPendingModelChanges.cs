using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Branches_TargetBranchId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Warehouses_SourceWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Warehouses_TargetWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferItems_Inventories_SourceInventoryId",
                table: "StockTransferItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferItems_Inventories_TargetInventoryId",
                table: "StockTransferItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_Warehouses_SourceWarehouseId",
                table: "StockTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_Warehouses_TargetWarehouseId",
                table: "StockTransfers");

            migrationBuilder.RenameColumn(
                name: "TargetWarehouseId",
                table: "StockTransfers",
                newName: "ToWarehouseId");

            migrationBuilder.RenameColumn(
                name: "SourceWarehouseId",
                table: "StockTransfers",
                newName: "FromWarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_StockTransfers_TargetWarehouseId",
                table: "StockTransfers",
                newName: "IX_StockTransfers_ToWarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_StockTransfers_SourceWarehouseId",
                table: "StockTransfers",
                newName: "IX_StockTransfers_FromWarehouseId");

            migrationBuilder.RenameColumn(
                name: "TargetInventoryId",
                table: "StockTransferItems",
                newName: "ToInventoryId");

            migrationBuilder.RenameColumn(
                name: "SourceInventoryId",
                table: "StockTransferItems",
                newName: "FromInventoryId");

            migrationBuilder.RenameIndex(
                name: "IX_StockTransferItems_TargetInventoryId",
                table: "StockTransferItems",
                newName: "IX_StockTransferItems_ToInventoryId");

            migrationBuilder.RenameIndex(
                name: "IX_StockTransferItems_SourceInventoryId",
                table: "StockTransferItems",
                newName: "IX_StockTransferItems_FromInventoryId");

            migrationBuilder.RenameColumn(
                name: "TargetWarehouseId",
                table: "StockMovements",
                newName: "ToWarehouseId");

            migrationBuilder.RenameColumn(
                name: "TargetBranchId",
                table: "StockMovements",
                newName: "ToBranchId");

            migrationBuilder.RenameColumn(
                name: "SourceWarehouseId",
                table: "StockMovements",
                newName: "FromWarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_StockMovements_TargetWarehouseId",
                table: "StockMovements",
                newName: "IX_StockMovements_ToWarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_StockMovements_TargetBranchId",
                table: "StockMovements",
                newName: "IX_StockMovements_ToBranchId");

            migrationBuilder.RenameIndex(
                name: "IX_StockMovements_SourceWarehouseId",
                table: "StockMovements",
                newName: "IX_StockMovements_FromWarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Branches_ToBranchId",
                table: "StockMovements",
                column: "ToBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Warehouses_FromWarehouseId",
                table: "StockMovements",
                column: "FromWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Warehouses_ToWarehouseId",
                table: "StockMovements",
                column: "ToWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferItems_Inventories_FromInventoryId",
                table: "StockTransferItems",
                column: "FromInventoryId",
                principalTable: "Inventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferItems_Inventories_ToInventoryId",
                table: "StockTransferItems",
                column: "ToInventoryId",
                principalTable: "Inventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_Warehouses_FromWarehouseId",
                table: "StockTransfers",
                column: "FromWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_Warehouses_ToWarehouseId",
                table: "StockTransfers",
                column: "ToWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Branches_ToBranchId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Warehouses_FromWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Warehouses_ToWarehouseId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferItems_Inventories_FromInventoryId",
                table: "StockTransferItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransferItems_Inventories_ToInventoryId",
                table: "StockTransferItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_Warehouses_FromWarehouseId",
                table: "StockTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_Warehouses_ToWarehouseId",
                table: "StockTransfers");

            migrationBuilder.RenameColumn(
                name: "ToWarehouseId",
                table: "StockTransfers",
                newName: "TargetWarehouseId");

            migrationBuilder.RenameColumn(
                name: "FromWarehouseId",
                table: "StockTransfers",
                newName: "SourceWarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_StockTransfers_ToWarehouseId",
                table: "StockTransfers",
                newName: "IX_StockTransfers_TargetWarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_StockTransfers_FromWarehouseId",
                table: "StockTransfers",
                newName: "IX_StockTransfers_SourceWarehouseId");

            migrationBuilder.RenameColumn(
                name: "ToInventoryId",
                table: "StockTransferItems",
                newName: "TargetInventoryId");

            migrationBuilder.RenameColumn(
                name: "FromInventoryId",
                table: "StockTransferItems",
                newName: "SourceInventoryId");

            migrationBuilder.RenameIndex(
                name: "IX_StockTransferItems_ToInventoryId",
                table: "StockTransferItems",
                newName: "IX_StockTransferItems_TargetInventoryId");

            migrationBuilder.RenameIndex(
                name: "IX_StockTransferItems_FromInventoryId",
                table: "StockTransferItems",
                newName: "IX_StockTransferItems_SourceInventoryId");

            migrationBuilder.RenameColumn(
                name: "ToWarehouseId",
                table: "StockMovements",
                newName: "TargetWarehouseId");

            migrationBuilder.RenameColumn(
                name: "ToBranchId",
                table: "StockMovements",
                newName: "TargetBranchId");

            migrationBuilder.RenameColumn(
                name: "FromWarehouseId",
                table: "StockMovements",
                newName: "SourceWarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_StockMovements_ToWarehouseId",
                table: "StockMovements",
                newName: "IX_StockMovements_TargetWarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_StockMovements_ToBranchId",
                table: "StockMovements",
                newName: "IX_StockMovements_TargetBranchId");

            migrationBuilder.RenameIndex(
                name: "IX_StockMovements_FromWarehouseId",
                table: "StockMovements",
                newName: "IX_StockMovements_SourceWarehouseId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferItems_Inventories_SourceInventoryId",
                table: "StockTransferItems",
                column: "SourceInventoryId",
                principalTable: "Inventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransferItems_Inventories_TargetInventoryId",
                table: "StockTransferItems",
                column: "TargetInventoryId",
                principalTable: "Inventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_Warehouses_SourceWarehouseId",
                table: "StockTransfers",
                column: "SourceWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_Warehouses_TargetWarehouseId",
                table: "StockTransfers",
                column: "TargetWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
