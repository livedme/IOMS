using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class ProductDtoMappingProfile : Profile
{
    public ProductDtoMappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ConstructUsing(s => new ProductDto(
                s.Id,
                s.Name,
                s.SKU,
                s.Barcode,
                s.Description,
                s.CostPrice,
                s.SellingPrice,
                s.WholeSellingPrice,
                s.ReorderStockLevel,
                s.MinOrderQuantity,
                s.CategoryId,
                s.Category != null ? s.Category.Name : null,
                s.BrandId,
                s.Brand != null ? s.Brand.Name : null,
                s.Model,
                s.ImageUrl,
                s.Inventories.Sum(i => i.Quantity),
                s.IsKit,
                s.OriginCountry ?? string.Empty,
                s.OriginManufacturer ?? string.Empty))
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand != null ? s.Brand.Name : null))
            .ForMember(d => d.TotalStock, o => o.MapFrom(s => s.Inventories.Sum(i => i.Quantity)));
    }
}
