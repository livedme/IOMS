using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class StocktakeItemDtoMappingProfile : Profile
{
    public StocktakeItemDtoMappingProfile()
    {
        CreateMap<StocktakeItem, StocktakeItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSKU, o => o.MapFrom(s => s.Product.SKU))
            .ForMember(d => d.Variance, o => o.MapFrom(s => (s.CountedQuantity ?? 0) - s.SystemQuantity));
    }
}
