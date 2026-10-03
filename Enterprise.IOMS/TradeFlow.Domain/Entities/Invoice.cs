using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue => TotalAmount - PaidAmount;
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public decimal ExchangeRate { get; set; } = 1;
    public string? Notes { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();
    public ICollection<DebitNote> DebitNotes { get; set; } = new List<DebitNote>();
}
