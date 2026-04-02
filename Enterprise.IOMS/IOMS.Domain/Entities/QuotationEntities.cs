using IOMS.Domain.Enums;

namespace IOMS.Domain.Entities;

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
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public string? Notes { get; set; }
    public Guid? ConvertedToOrderId { get; set; }
    public ICollection<SalesQuoteItem> Items { get; set; } = new List<SalesQuoteItem>();
}

public class SalesQuoteItem : BaseEntity
{
    public Guid SalesQuoteId { get; set; }
    public SalesQuote SalesQuote { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class RfqRequest : BaseEntity
{
    public string RfqNumber { get; set; } = string.Empty;
    public RfqStatus Status { get; set; } = RfqStatus.Draft;
    public DateTime? RequiredDate { get; set; }
    public string? Notes { get; set; }
    public ICollection<RfqItem> Items { get; set; } = new List<RfqItem>();
    public ICollection<RfqSupplierResponse> SupplierResponses { get; set; } = new List<RfqSupplierResponse>();
}

public class RfqItem : BaseEntity
{
    public Guid RfqRequestId { get; set; }
    public RfqRequest RfqRequest { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal? TargetUnitPrice { get; set; }
}

public class RfqSupplierResponse : BaseEntity
{
    public Guid RfqRequestId { get; set; }
    public RfqRequest RfqRequest { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public decimal QuotedPrice { get; set; }
    public int LeadTimeDays { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Notes { get; set; }
    public bool IsSelected { get; set; }
}
