using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class ProductDetailsDtoMappingProfile : Profile
{
    public ProductDetailsDtoMappingProfile()
    {
        CreateMap<Product, ProductDetailsDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.TotalStock, o => o.MapFrom(s => s.Inventories.Sum(i => i.Quantity)))
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand != null ? s.Brand.Name : null))
            .ForMember(d => d.Model, o => o.MapFrom(s => s.Model));
    }
}
