using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class UoMConversionDtoMappingProfile : Profile
{
    public UoMConversionDtoMappingProfile()
    {
        CreateMap<UoMConversion, UoMConversionDto>()
            .ForMember(d => d.FromUoMName, o => o.MapFrom(s => s.FromUoM.Name))
            .ForMember(d => d.ToUoMName, o => o.MapFrom(s => s.ToUoM.Name));
    }
}
