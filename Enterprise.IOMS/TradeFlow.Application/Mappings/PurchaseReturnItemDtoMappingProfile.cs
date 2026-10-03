using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;
using static TradeFlow.Application.DTOs.PurchaseReturnDto;

namespace TradeFlow.Application.Mappings;

public class PurchaseReturnItemDtoMappingProfile : Profile
{
    public PurchaseReturnItemDtoMappingProfile()
    {
        CreateMap<PurchaseReturnItem, PurchaseReturnItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));
    }
}
