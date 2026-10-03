using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class WarehouseDtoMappingProfile : Profile
{
    public WarehouseDtoMappingProfile()
    {
        CreateMap<Warehouse, WarehouseDto>();
    }
}
