using IOMS.Domain.Enums;

namespace IOMS.Application.DTOs
{

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
        public DiscountType DiscountType { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public FilterItem SelectedCustomer { get; set; } = new FilterItem(Guid.Empty, "", false);
        public SalesOrderItemDtoModels SelectedItem { get; set; } = new SalesOrderItemDtoModels();
        public List<CreateSalesOrderItemDto> Items { get; set; } = new();

        //Guid ProductId, int Quantity, decimal UnitPrice,decimal DiscountAmount, DiscountType DiscountType, decimal TotalPrice, Guid? UoMId
        public class SalesOrderItemDtoModels
        {
            public FilterItem SelectedProduct { get; set; } = new FilterItem(Guid.Empty, "", false);
            public Guid ProductId { get; set; }
            public int Quantity { get; set; }
            public Guid Serial{ get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountAmount { get; set; }
            public int DiscountType { get; set; }
            public decimal TotalDiscount { get; set; }
            public decimal TotalPrice { get; set; }
            public List<Guid> SelectedSerialIds { get; set; } = new();
        }
    }
    
    
    public class PurchaseOrderDtoModels
    {
        public DateTime? PurchaseDate { get; set; } = DateTime.UtcNow;
        public Guid SupplierId { get; set; }
        public FilterItem SelectedSupplier { get; set; } = new FilterItem(Guid.Empty, "", false);
        public string? SupplierText { get; set; }
        public FilterItem? LastSupplierSelection { get; set; }
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

        public PurchaseOrderItemDtoModels SelectedItem { get; set; } = new PurchaseOrderItemDtoModels();
        
        public List<CreatePurchaseOrderItemDto> Items { get; set; } = new();

        public class PurchaseOrderItemDtoModels
        {
            public Guid Id { get; set; }
            public FilterItem SelectedProduct { get; set; } = new FilterItem(Guid.Empty, "", false);
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