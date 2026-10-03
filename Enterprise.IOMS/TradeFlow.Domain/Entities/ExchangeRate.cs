namespace TradeFlow.Domain.Entities;

public class ExchangeRate : BaseEntity
{
    public Guid FromCurrencyId { get; set; }
    public Currency FromCurrency { get; set; } = null!;
    public Guid ToCurrencyId { get; set; }
    public Currency ToCurrency { get; set; } = null!;
    public decimal Rate { get; set; }
    public DateTime EffectiveDate { get; set; }
}
