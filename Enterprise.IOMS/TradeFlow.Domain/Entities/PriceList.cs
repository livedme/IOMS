namespace TradeFlow.Domain.Entities;

public class PriceList : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
}
