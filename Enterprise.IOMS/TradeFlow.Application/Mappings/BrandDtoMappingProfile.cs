using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class BrandDtoMappingProfile : Profile
{
    public BrandDtoMappingProfile()
    {
        CreateMap<Brand, BrandDto>()
              .ConstructUsing(s => new BrandDto(
                  s.Id,
                  s.Name,
                  s.BrandCode,
                  s.Description,
                  s.Products.Count))
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Products.Count));
    }
}
