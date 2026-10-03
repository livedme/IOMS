namespace TradeFlow.Domain.Entities;

public class TaxJurisdiction : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? State { get; set; }
    public string? Description { get; set; }
    public ICollection<TaxRate> TaxRates { get; set; } = new List<TaxRate>();
}
