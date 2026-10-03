namespace TradeFlow.Domain.Entities;

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
