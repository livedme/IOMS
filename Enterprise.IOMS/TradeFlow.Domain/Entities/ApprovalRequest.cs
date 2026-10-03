using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class ApprovalRequest : BaseEntity
{
    public string DocumentType { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public ApprovalDecision Status { get; set; } = ApprovalDecision.Pending;
    public int CurrentLevel { get; set; }
    public ICollection<ApprovalRequestStep> Steps { get; set; } = new List<ApprovalRequestStep>();
}
