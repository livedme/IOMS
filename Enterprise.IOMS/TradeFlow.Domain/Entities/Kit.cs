namespace TradeFlow.Domain.Entities;

public class Kit : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<KitComponent> Components { get; set; } = new List<KitComponent>();
}
