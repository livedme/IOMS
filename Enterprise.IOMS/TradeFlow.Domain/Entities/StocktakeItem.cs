namespace TradeFlow.Domain.Entities;

public class StocktakeItem : BaseEntity
{
    public Guid StocktakeId { get; set; }
    public Stocktake Stocktake { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int SystemQuantity { get; set; }
    public int? CountedQuantity { get; set; }
    public int Variance => (CountedQuantity ?? 0) - SystemQuantity;
    public string? Notes { get; set; }
}
