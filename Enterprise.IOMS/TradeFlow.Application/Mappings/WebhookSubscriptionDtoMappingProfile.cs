using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class WebhookSubscriptionDtoMappingProfile : Profile
{
    public WebhookSubscriptionDtoMappingProfile()
    {
        CreateMap<WebhookSubscription, WebhookSubscriptionDto>()
            .ForCtorParam("DeliveryCount", o => o.MapFrom(s => s.DeliveryLogs.Count));
    }
}
