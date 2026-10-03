using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class StocktakeDtoMappingProfile : Profile
{
    public StocktakeDtoMappingProfile()
    {
        CreateMap<Stocktake, StocktakeDto>()
            .ForCtorParam("WarehouseName", o => o.MapFrom(s => s.Warehouse != null ? s.Warehouse.Name : string.Empty))
            .ForCtorParam("TotalItems", o => o.MapFrom(s => s.Items.Count))
            .ForCtorParam("CountedItems", o => o.MapFrom(s => s.Items.Count(i => i.CountedQuantity.HasValue)));
    }
}
