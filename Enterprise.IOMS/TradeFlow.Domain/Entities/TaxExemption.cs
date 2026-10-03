namespace TradeFlow.Domain.Entities;

public class TaxExemption : BaseEntity
{
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public string ExemptionCode { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime? ValidUntil { get; set; }
}
