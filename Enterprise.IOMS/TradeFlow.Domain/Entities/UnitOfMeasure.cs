namespace TradeFlow.Domain.Entities;

public class UnitOfMeasure : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public bool IsBaseUnit { get; set; }
    public ICollection<UoMConversion> FromConversions { get; set; } = new List<UoMConversion>();
    public ICollection<UoMConversion> ToConversions { get; set; } = new List<UoMConversion>();
}
