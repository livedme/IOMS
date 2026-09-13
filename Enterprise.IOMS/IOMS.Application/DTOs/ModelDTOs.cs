using IOMS.Domain.Enums;

namespace IOMS.Application.DTOs
{

    public class ProductDetailsDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string SKU { get; set; }
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal WholeSellingPrice { get; set; }
        public int ReorderStockLevel { get; set; }
        public int MinOrderQuantity { get; set; }        
        public ControlDto CategoryControl { get; set; }= new ControlDto();
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public ControlDto BrandControl { get; set; }= new ControlDto();
        public Guid? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? ImageUrl { get; set; }
        public int TotalStock { get; set; }
        public bool IsKit { get; set; }
        public string? Model { get; set; }
        public Guid? BaseUoMId { get; set; }
        public string OriginCountry { get; set; }
        public string OriginManufacturer { get; set; }
    }

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