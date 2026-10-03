using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{
    // Inventory
    public record InventoryDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU, Guid WarehouseId, string WarehouseName, 
        int Quantity, int ReservedQuantity, int AvailableQuantity, string? BinLocation);

    public record StockLevelDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU, Guid WarehouseId, string WarehouseName, int Quantity, 
        int ReservedQuantity, int AvailableQuantity, int ReorderStockLevel, string? BinLocation);

    public record StockMovementDto(Guid Id, DateTime MovementDate, Guid ProductId, string ProductName, Guid WarehouseId, string WarehouseName, 
        StockMovementType Type, int Quantity, string? Reference, string? Notes, Guid? SourceWarehouseId, Guid? DestinationWarehouseId);

    // Extended InventoryDto for tracking
    public record InventoryTrackingDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU, Guid WarehouseId, string WarehouseName, int Quantity,
        int ReservedQuantity, int AvailableQuantity, string? BinLocation, string? SerialNumber, string? BatchNumber, DateTime? ExpiryDate, string? Condition);

    public record StockAdjustmentDto(Guid ProductId, Guid WarehouseId, int QuantityChange, string Reason);
    public record StockTransferDto(Guid ProductId, Guid SourceWarehouseId, Guid TargetWarehouseId, int Quantity);
}
