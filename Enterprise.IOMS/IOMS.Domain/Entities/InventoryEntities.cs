using IOMS.Domain.Enums;

namespace IOMS.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal WholeSellingPrice { get; set; }
    public int ReorderLevel { get; set; }
    public int MinimumOrderQuantity { get; set; } = 1;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid? BaseUoMId { get; set; }
    public UnitOfMeasure? BaseUoM { get; set; }

    public int BrandId { get; set; }
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
}

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Path { get; set; }    
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Warehouse : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

public class Inventory : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public DateTime? LastStockDate { get; set; }
    public string? BinLocation { get; set; }

    // Inventory tracking enhancements
    public string? SerialNumber { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Condition { get; set; } // New, Used, Refurbished
}

public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public Guid? SourceWarehouseId { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
}

public class UnitOfMeasure : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public bool IsBaseUnit { get; set; }
    public ICollection<UoMConversion> FromConversions { get; set; } = new List<UoMConversion>();
    public ICollection<UoMConversion> ToConversions { get; set; } = new List<UoMConversion>();
}

public class UoMConversion : BaseEntity
{
    public Guid FromUoMId { get; set; }
    public UnitOfMeasure FromUoM { get; set; } = null!;
    public Guid ToUoMId { get; set; }
    public UnitOfMeasure ToUoM { get; set; } = null!;
    public decimal ConversionFactor { get; set; }
}
