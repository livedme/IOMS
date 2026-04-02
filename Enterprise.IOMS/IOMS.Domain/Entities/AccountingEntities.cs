using IOMS.Domain.Enums;

namespace IOMS.Domain.Entities;

public class Account : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public Guid? ParentAccountId { get; set; }
    public Account? ParentAccount { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystemAccount { get; set; }
    public string? Description { get; set; }
    public ICollection<Account> SubAccounts { get; set; } = new List<Account>();
    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
}

public class JournalEntry : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public bool IsAutoPosted { get; set; }
    public bool IsReversed { get; set; }
    public Guid? ReversalOfId { get; set; }
    public JournalEntry? ReversalOf { get; set; }
    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}

public class JournalEntryLine : BaseEntity
{
    public Guid JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}

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

public class DebitNote : BaseEntity
{
    public string DebitNoteNumber { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public DebitNoteStatus Status { get; set; } = DebitNoteStatus.Draft;
}

public class BankStatement : BaseEntity
{
    public string BankAccountName { get; set; } = string.Empty;
    public DateTime StatementDate { get; set; }
    public string? FileName { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public bool IsReconciled { get; set; }
    public ICollection<BankStatementLine> Lines { get; set; } = new List<BankStatementLine>();
}

public class BankStatementLine : BaseEntity
{
    public Guid BankStatementId { get; set; }
    public BankStatement BankStatement { get; set; } = null!;
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public Guid? MatchedPaymentId { get; set; }
    public Payment? MatchedPayment { get; set; }
    public bool IsMatched { get; set; }
}
