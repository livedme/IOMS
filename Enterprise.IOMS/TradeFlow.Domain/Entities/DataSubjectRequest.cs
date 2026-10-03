using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class DataSubjectRequest : BaseEntity
{
    public DataSubjectRequestType RequestType { get; set; }
    public string SubjectEmail { get; set; } = string.Empty;
    public DataSubjectRequestStatus Status { get; set; } = DataSubjectRequestStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}
