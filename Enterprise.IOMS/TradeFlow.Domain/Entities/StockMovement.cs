using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; } = null!;
    public Guid? ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; } = null!;
    public Guid? ToBranchId { get; set; }
    public Branch? ToBranch { get; set; } = null!;
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
}
