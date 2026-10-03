using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IWebhookService
{
    Task<List<WebhookSubscriptionDto>> GetSubscriptions();
    Task<Guid> CreateSubscription(CreateWebhookSubscriptionDto dto);
    Task DeleteSubscription(Guid id);
    Task<List<WebhookDeliveryLogDto>> GetDeliveryLogs(Guid subscriptionId);
}
