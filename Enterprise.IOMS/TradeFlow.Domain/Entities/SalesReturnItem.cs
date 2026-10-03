namespace TradeFlow.Domain.Entities;

public class SalesReturnItem : BaseEntity
{
    public Guid SalesReturnId { get; set; }
    public SalesReturn SalesReturn { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
