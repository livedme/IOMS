namespace TradeFlow.Domain.Entities;

public class ApprovalWorkflowRule : BaseEntity
{
    public string DocumentType { get; set; } = string.Empty;
    public string? Condition { get; set; }
    public decimal? AmountThreshold { get; set; }
    public string? ApproverRoleOrUserId { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; } = true;
}
