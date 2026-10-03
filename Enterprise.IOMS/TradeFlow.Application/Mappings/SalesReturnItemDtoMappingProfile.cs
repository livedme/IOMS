using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class SalesReturnItemDtoMappingProfile : Profile
{
    public SalesReturnItemDtoMappingProfile()
    {
        CreateMap<SalesReturnItem, SalesReturnItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));
    }
}
