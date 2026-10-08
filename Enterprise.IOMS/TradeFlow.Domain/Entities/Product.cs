namespace TradeFlow.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal WholeSellingPrice { get; set; }
    public int ReorderStockLevel { get; set; }
    public int MinOrderQuantity { get; set; }
    public int WarrantyInMonths { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid? BaseUoMId { get; set; }
    public UnitOfMeasure? BaseUoM { get; set; }
    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public string? Model { get; set; }
    public string? Unit { get; set; }
    public string? OriginManufacturer { get; set; }
    public string? OriginCountry { get; set; }
    public bool IsKit { get; set; }
    public bool IsTaxExempt { get; set; }
    public string? ImageUrl { get; set; }

    public decimal Weight { get; set; }
    public decimal Volume { get; set; }
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<PriceListItem> PriceListItems { get; set; } = new List<PriceListItem>();

    // --- General/Extensible attributes ---
    public string? Specifications { get; set; }         // Additional specs (JSON or delimited)
    public ICollection<ProductSerial> Serials { get; set; } = new List<ProductSerial>();
}
