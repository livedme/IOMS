namespace TradeFlow.Domain.Entities;

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
