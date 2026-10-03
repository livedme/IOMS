using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class CreditNote : BaseEntity
{
    public string CreditNoteNumber { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public CreditNoteStatus Status { get; set; } = CreditNoteStatus.Draft;
}
