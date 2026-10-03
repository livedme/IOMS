using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class InventoryDtoMappingProfile : Profile
{
    public InventoryDtoMappingProfile()
    {
        CreateMap<Inventory, InventoryDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSKU, o => o.MapFrom(s => s.Product.SKU))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.Name))
            .ForMember(d => d.AvailableQuantity, o => o.MapFrom(s => s.Quantity - s.ReservedQuantity));
    }
}
