namespace TradeFlow.Application.DTOs;

public record WebhookSubscriptionDto(Guid Id, string EventType, string Url, bool IsActive, int DeliveryCount);
public record CreateWebhookSubscriptionDto(string EventType, string Url, string Secret);
public record WebhookDeliveryLogDto(Guid Id, string EventType, int? StatusCode, int Attempt,
    DateTime SentAt, bool IsSuccess);
