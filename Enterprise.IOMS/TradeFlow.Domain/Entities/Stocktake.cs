using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class Stocktake : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public StocktakeStatus Status { get; set; } = StocktakeStatus.Draft;
    public string? Notes { get; set; }
    public ICollection<StocktakeItem> Items { get; set; } = new List<StocktakeItem>();
}
