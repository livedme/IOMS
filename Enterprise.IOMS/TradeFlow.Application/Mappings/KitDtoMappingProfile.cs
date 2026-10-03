using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class KitDtoMappingProfile : Profile
{
    public KitDtoMappingProfile()
    {
        CreateMap<Kit, KitDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSKU, o => o.MapFrom(s => s.Product.SKU));
    }
}
