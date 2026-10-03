namespace TradeFlow.Domain.Entities;

public class BankStatement : BaseEntity
{
    public string BankAccountName { get; set; } = string.Empty;
    public DateTime StatementDate { get; set; }
    public string? FileName { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public bool IsReconciled { get; set; }
    public ICollection<BankStatementLine> Lines { get; set; } = new List<BankStatementLine>();
}
