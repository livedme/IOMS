using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class AuditLogDtoMappingProfile : Profile
{
    public AuditLogDtoMappingProfile()
    {
        CreateMap<AuditLog, AuditLogDto>();
    }
}
