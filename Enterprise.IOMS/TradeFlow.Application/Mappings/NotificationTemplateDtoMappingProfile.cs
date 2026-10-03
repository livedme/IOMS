using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class NotificationTemplateDtoMappingProfile : Profile
{
    public NotificationTemplateDtoMappingProfile()
    {
        CreateMap<NotificationTemplate, NotificationTemplateDto>();
    }
}
