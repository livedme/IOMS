namespace TradeFlow.Domain.Entities;

public class WebhookDeliveryLog : BaseEntity
{
    public Guid SubscriptionId { get; set; }
    public WebhookSubscription Subscription { get; set; } = null!;
    public string EventType { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public int? StatusCode { get; set; }
    public int Attempt { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; }
}
