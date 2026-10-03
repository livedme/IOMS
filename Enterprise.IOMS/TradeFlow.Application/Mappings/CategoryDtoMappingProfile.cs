using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class CategoryDtoMappingProfile : Profile
{
    public CategoryDtoMappingProfile()
    {
        CreateMap<Category, CategoryDto>()
            .ConstructUsing(s => new CategoryDto(
                s.Id,
                s.Name,
                s.Description,
                s.ParentCategoryId,
                s.ParentCategory != null ? s.ParentCategory.Name : null,
                s.Products.Count,
                s.Path))
            .ForMember(d => d.ParentCategoryName, o => o.MapFrom(s => s.ParentCategory != null ? s.ParentCategory.Name : null))
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Products.Count));
    }
}
