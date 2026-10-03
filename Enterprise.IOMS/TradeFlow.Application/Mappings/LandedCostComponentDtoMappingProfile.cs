using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class LandedCostComponentDtoMappingProfile : Profile
{
    public LandedCostComponentDtoMappingProfile()
    {
        CreateMap<LandedCostComponent, LandedCostComponentDto>();
    }
}
