namespace TradeFlow.Domain.Entities;

public class UoMConversion : BaseEntity
{
    public Guid FromUoMId { get; set; }
    public UnitOfMeasure FromUoM { get; set; } = null!;
    public Guid ToUoMId { get; set; }
    public UnitOfMeasure ToUoM { get; set; } = null!;
    public decimal ConversionFactor { get; set; }
}
