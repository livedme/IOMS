using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class LandedCostAllocationDtoMappingProfile : Profile
{
    public LandedCostAllocationDtoMappingProfile()
    {
        CreateMap<LandedCostAllocation, LandedCostAllocationDto>()
            .ForCtorParam("PurchaseOrderNumber", o => o.MapFrom(s => s.PurchaseOrder != null ? s.PurchaseOrder.OrderNumber : null))
            .ForCtorParam("ComponentName", o => o.MapFrom(s => s.Component != null ? s.Component.Name : string.Empty));
    }
}
