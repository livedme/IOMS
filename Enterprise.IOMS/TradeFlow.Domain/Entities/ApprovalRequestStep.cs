using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class ApprovalRequestStep : BaseEntity
{
    public Guid ApprovalRequestId { get; set; }
    public ApprovalRequest ApprovalRequest { get; set; } = null!;
    public string ApproverId { get; set; } = string.Empty;
    public int Level { get; set; }
    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;
    public DateTime? DecidedAt { get; set; }
    public string? Notes { get; set; }
}
