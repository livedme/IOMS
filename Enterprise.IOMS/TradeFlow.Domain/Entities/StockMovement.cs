using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

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
