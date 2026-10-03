using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class SalesQuoteItem : BaseEntity
{
    public Guid SalesQuoteId { get; set; }
    public SalesQuote SalesQuote { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}
