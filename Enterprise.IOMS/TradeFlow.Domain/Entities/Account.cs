using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

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
