using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class SalesQuote : BaseEntity
{
    public string QuoteNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
    public DateTime QuoteDate { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }
    public int Version { get; set; } = 1;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public DiscountType DiscountType { get; set; }    
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public string? Notes { get; set; }
    public Guid? ConvertedToOrderId { get; set; }
    public ICollection<SalesQuoteItem> Items { get; set; } = new List<SalesQuoteItem>();
}
