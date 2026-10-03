namespace TradeFlow.Domain.Entities;

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
