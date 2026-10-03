using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class TaxRate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public TaxType TaxType { get; set; }
    public Guid? TaxJurisdictionId { get; set; }
    public TaxJurisdiction? TaxJurisdiction { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCompound { get; set; }
}
