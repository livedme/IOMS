using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class UnitOfMeasureDtoMappingProfile : Profile
{
    public UnitOfMeasureDtoMappingProfile()
    {
        CreateMap<UnitOfMeasure, UnitOfMeasureDto>();
    }
}
