using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class WebhookDeliveryLogDtoMappingProfile : Profile
{
    public WebhookDeliveryLogDtoMappingProfile()
    {
        CreateMap<WebhookDeliveryLog, WebhookDeliveryLogDto>();
    }
}
