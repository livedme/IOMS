using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Domain.Enums;
namespace TradeFlow.Application.DTOs
{
    // Inventory
    public record InventoryDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU, Guid? WarehouseId, string? WarehouseName,
        Guid? BranchId, string? BranchName, int Quantity, int ReservedQuantity, int AvailableQuantity, string? BinLocation);

    public record StockLevelDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU, Guid? WarehouseId, string? WarehouseName,
        Guid? BranchId, string? BranchName, int Quantity, int ReservedQuantity, int AvailableQuantity, int ReorderStockLevel, string? BinLocation);

    public record StockMovementDto(Guid Id, DateTime MovementDate, Guid ProductId, string ProductName, Guid? ToWarehouseId, string? ToWarehouseName,
        Guid? BranchId, string? BranchName, StockMovementType Type, int Quantity, string? Reference, string? Notes, Guid? FromWarehouseId, Guid? FromWarehouseName);

    // Extended InventoryDto for tracking
    public record InventoryTrackingDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU, Guid? WarehouseId, string? WarehouseName,
        Guid? BranchId, string? BranchName, int Quantity, int ReservedQuantity, int AvailableQuantity, string? BinLocation,
        string? SerialNumber, string? BatchNumber, DateTime? ExpiryDate, string? Condition);
     
    public record StockAdjustmentDto(Guid ProductId, Guid WarehouseId, int QuantityChange, string Reason);
    public record StockTransferDto(Guid ProductId, Guid FromWarehouseId, Guid ToWarehouseId, int Quantity);

    public record StockTransferRequestDto(Guid ProductId, Guid FromWarehouseId, Guid TargetWarehouseId, int Quantity, string? Notes);

    public record StockTransferLineDto(Guid Id, Guid ProductId, int Quantity, Guid FromWarehouseId = default);

    public record StockTransferItemDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU, int Quantity, string FromWarehouseName);

    public record StockTransferListDto(Guid Id, string TransferNumber, DateTime TransferDate,
        Guid FromWarehouseId, string FromWarehouseName, Guid ToWarehouseId, string ToWarehouseName,
        StockTransferStatus Status, string? Notes, int TotalLines, int TotalQuantity, List<StockTransferItemDto> Items);

    //public record StockTransferCreateDto(Guid Id, Guid FromWarehouseId, Guid ToWarehouseId, string? Notes, List<StockTransferLineDto> Items, DateTime? TransferDate = null);

    public class StockTransferDtoModel
    {
        public Guid Id { get; set; }
        public DateTime? TransferDate { get; set; } = DateTime.UtcNow;
        public Guid ToWarehouseId { get; set; }
        public string? ToWarehouseText { get; set; }
        public string? Notes { get; set; }

        public StockTransferLineEntry ProductObj { get; set; } = new StockTransferLineEntry();

        public List<StockTransferLineDto> ItemsLine { get; set; } = new();

        public class StockTransferLineEntry
        {
            public Guid Id { get; set; }
            public ControlDto ProductDdlControl { get; set; } = new ControlDto();
            public Guid ProductId { get; set; }
            public Guid WarehouseId { get; set; }
            public int Quantity { get; set; } = 1;
        }
    }

    // Dead Stock
    public record DeadStockItemDto(Guid ProductId, string ProductName, string SKU, string WarehouseName,
        int Quantity, decimal Value, DateTime? LastMovementDate, int DaysSinceLastMovement);

}
