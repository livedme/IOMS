using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class ScheduledReportDtoMappingProfile : Profile
{
    public ScheduledReportDtoMappingProfile()
    {
        CreateMap<ScheduledReport, ScheduledReportDto>();
    }
}
