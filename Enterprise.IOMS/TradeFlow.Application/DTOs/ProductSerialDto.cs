using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;

public record ProductSerialDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string SerialNumber,
    string? Barcode,
    string? QRCode,
    DateTime? PurchaseDate,
    DateTime? WarrantyStartDate,
    DateTime? WarrantyEndDate,
    ProductSerialStatus Status,
    Guid? WarehouseId,
    string? WarehouseName,
    string? BinLocation,
    Guid? SupplierId,
    string? SupplierName,
    DateTime CreatedAt);

public record CreateProductSerialDto(
    Guid ProductId,
    string SerialNumber,
    string? Barcode,
    string? QRCode,
    DateTime? PurchaseDate,
    DateTime? WarrantyStartDate,
    DateTime? WarrantyEndDate,
    Guid? WarehouseId,
    string? BinLocation,
    Guid? SupplierId);
