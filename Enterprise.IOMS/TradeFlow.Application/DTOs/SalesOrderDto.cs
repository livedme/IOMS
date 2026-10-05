using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs
{

    public record SalesOrderDto(Guid Id, string OrderNumber, Guid CustomerId, CustomerDto Customer, Guid? BranchId, BranchDto? Branch,
        DateTime OrderDate, OrderStatus Status, string Naration, string Chalan, decimal SubTotal, decimal TruckCharge, decimal LabourCharge, decimal TaxAmount,
        decimal DiscountAmount, DiscountType DiscountType, decimal TotalAmount, decimal PaidAmount, decimal DueAmount, string? Notes, decimal ExchangeRate,
        DateTime? ExpectedDeliveryDate, List<SalesOrderItemDto> Items,int ItemCount = 0);

    public record SalesOrderItemDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU, int Quantity, int ShippedQuantity, decimal UnitPrice,

        decimal DiscountAmount, DiscountType DiscountType, decimal TotalDiscount, decimal LineTotalPrice);
    public record CreateSalesOrderDto(Guid CustomerId, Guid? WarehouseId, Guid? BranchId, string? Notes, string? ShippingAddress, DateTime? ExpectedDeliveryDate,
        Guid? CurrencyId, OrderStatus Status, DateTime SalesDate, decimal SubTotal, decimal LabourCharge, decimal TruckCharge, decimal TaxAmount, int DiscountType,
        decimal DiscountAmount, decimal TotalAmount, decimal PaidAmount, decimal DueAmount, List<CreateSalesOrderItemDto> Items);

    public record CreateSalesOrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice, decimal DiscountAmount, int DiscountType, decimal TotalDiscountAmount, 
        decimal TotalPrice, Guid? UoMId, List<Guid>? SerialIds = null);

    public class CreateSalesOrderDtoModels
    {
        public string? BranchName { get; set; }
        public Guid BranchId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string? Notes { get; set; }
        public string? ShippingAddress { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public Guid? CurrencyId { get; set; }
        public OrderStatus? Status { get; set; }
        public DateTime? SalesDate { get; set; } = DateTime.UtcNow;
        public string? Naration { get; set; }
        public string? Chalan { get; set; }
        public decimal DrAmount { get; set; }
        public decimal CrAmount { get; set; }
        public int StackQty { get; set; }
        public decimal SubTotal { get; set; }
        public decimal LabourCharge { get; set; }
        public decimal TruckCharge { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public int DiscountType { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public ControlDto CustomerDdlControl { get; set; } = new ControlDto();
        //public ControlDto SelectedCustomer { get; set; } = new FilterItem(Guid.Empty, "", false);
        public SalesOrderItemDtoModels ProductObj { get; set; } = new SalesOrderItemDtoModels();
        public List<CreateSalesOrderItemDto> ItemsLine { get; set; } = new();


        public class SalesOrderItemDtoModels
        {
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
