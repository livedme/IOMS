using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class NotificationLog : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string? Status { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
}
