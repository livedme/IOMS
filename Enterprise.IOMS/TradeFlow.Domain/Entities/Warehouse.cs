namespace TradeFlow.Domain.Entities;

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
