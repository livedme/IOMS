using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class KitComponentDtoMappingProfile : Profile
{
    public KitComponentDtoMappingProfile()
    {
        CreateMap<KitComponent, KitComponentDto>()
            .ForMember(d => d.ComponentProductName, o => o.MapFrom(s => s.ComponentProduct.Name))
            .ForMember(d => d.ComponentProductSKU, o => o.MapFrom(s => s.ComponentProduct.SKU));
    }
}
