namespace TradeFlow.Domain.Entities;

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
