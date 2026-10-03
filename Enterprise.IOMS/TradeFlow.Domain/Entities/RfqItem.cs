namespace TradeFlow.Domain.Entities;

public class RfqItem : BaseEntity
{
    public Guid RfqRequestId { get; set; }
    public RfqRequest RfqRequest { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal? TargetUnitPrice { get; set; }
}
