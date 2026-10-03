using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{

    public record PurchaseOrderDto(Guid Id, string OrderNumber, Guid SupplierId, string SupplierName, Guid WarehouseId, string WarehouseName, DateTime PurchaseDate,
        PurchaseOrderStatus Status, decimal SubTotal, decimal LabourCharge, decimal TruckCharge, decimal DiscountAmount, DiscountType DiscountType, decimal TotalAmount,
        decimal TaxAmount, decimal PaidAmount, decimal DueAmount, string? Notes, DateTime? ExpectedDeliveryDate, List<PurchaseOrderItemDto> Items);

    public record PurchaseOrderItemDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU, int Quantity, int ReceivedQuantity, decimal UnitPrice,
        decimal DiscountAmount, DiscountType DiscountType, decimal TotalDiscountAmount, decimal LineTotalPrice);

    public record CreatePurchaseOrderDto(Guid SupplierId, Guid? WarehouseId, string? Notes, DateTime? ExpectedDeliveryDate, Guid? CurrencyId, List<CreatePurchaseOrderItemDto> Items);

    public record PurchaseSerialEntryDto(string SerialNumber, string? Barcode = null, DateTime? WarrantyStartDate = null, DateTime? WarrantyEndDate = null, string? BinLocation = null);

    public record CreatePurchaseOrderItemDto(Guid Id, Guid ProductId, int Quantity, decimal UnitPrice, decimal DiscountAmount, DiscountType DiscountType, decimal TotalDiscountAmount, decimal TotalPrice, Guid? UoMId, List<PurchaseSerialEntryDto>? Serials = null);

    public record GoodsReceivedLineDto(Guid PurchaseOrderItemId, Guid ProductId, int ReceivedQuantity);

    public class PurchaseOrderDtoModels
    {
        public DateTime? PurchaseDate { get; set; } = DateTime.UtcNow;
        public Guid WarehouseId { get; set; }
        public string? WarehouseText { get; set; }
        public string? Notes { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public int DiscountType { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }

        public Guid SupplierId { get; set; }
        public ControlDto SupplierDdlControl { get; set; } = new ControlDto();

        public PurchaseOrderItemDtoModels ProductObj { get; set; } = new PurchaseOrderItemDtoModels();

        public List<CreatePurchaseOrderItemDto> ItemsLine { get; set; } = new();

        public class PurchaseOrderItemDtoModels
        {
            public Guid Id { get; set; }
            public ControlDto ProductDdlControl { get; set; } = new ControlDto();
            public Guid ProductId { get; set; }
            public int Quantity { get; set; }
            public Guid Serial { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountAmount { get; set; }
            public int DiscountType { get; set; }
            public decimal TotalDiscount { get; set; }
            public decimal TotalPrice { get; set; }
            public List<Guid> SelectedSerialIds { get; set; } = new();
        }
    }

}
