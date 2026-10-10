using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class PurchaseReturnDtoMappingProfile : Profile
{
    public PurchaseReturnDtoMappingProfile()
    {
        CreateMap<PurchaseReturn, PurchaseReturnDto>()
            .ForMember(d => d.PurchaseOrderNumber, o => o.MapFrom(s => s.PurchaseOrder.OrderNumber))
            .ForMember(d => d.ItemsLine, o => o.MapFrom(s => s.Items));
    }
}
