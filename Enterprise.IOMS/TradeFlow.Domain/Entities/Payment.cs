using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class Payment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public PaymentMethod PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public decimal ExchangeRate { get; set; } = 1;
    public decimal ExchangeGainLoss { get; set; }
    public string? Notes { get; set; }
    public Guid? MatchedBankStatementLineId { get; set; }
}
