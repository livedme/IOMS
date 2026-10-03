namespace TradeFlow.Domain.Entities;

public class KitComponent : BaseEntity
{
    public Guid KitId { get; set; }
    public Kit Kit { get; set; } = null!;
    public Guid ComponentProductId { get; set; }
    public Product ComponentProduct { get; set; } = null!;
    public int Quantity { get; set; }
}
