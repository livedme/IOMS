using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class RfqItemDtoMappingProfile : Profile
{
    public RfqItemDtoMappingProfile()
    {
        CreateMap<RfqItem, RfqItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));
    }
}
