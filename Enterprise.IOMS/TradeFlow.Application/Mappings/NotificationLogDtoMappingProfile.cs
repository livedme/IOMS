using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class NotificationLogDtoMappingProfile : Profile
{
    public NotificationLogDtoMappingProfile()
    {
        CreateMap<NotificationLog, NotificationLogDto>();
    }
}
