using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class NotificationTemplate : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
}
