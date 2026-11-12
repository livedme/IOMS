using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class StockTransferDtoMappingProfile : Profile
{
    public StockTransferDtoMappingProfile()
    {
        CreateMap<StockTransferItem, StockTransferItemDto>()
            .ForCtorParam("ProductName", o => o.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty))
            .ForCtorParam("ProductSKU", o => o.MapFrom(s => s.Product != null ? s.Product.SKU : string.Empty))
            .ForCtorParam("FromWarehouseName", o => o.MapFrom(s => s.FromInventory != null ? s.FromInventory.Warehouse.Name : string.Empty)); 
         
        CreateMap<StockTransfer, StockTransferListDto>()
            .ForCtorParam("FromWarehouseName", o => o.MapFrom(s => s.FromWarehouse != null ? s.FromWarehouse.Name : string.Empty))
            .ForCtorParam("ToWarehouseName", o => o.MapFrom(s => s.ToWarehouse != null ? s.ToWarehouse.Name : string.Empty))
            .ForCtorParam("TotalLines", o => o.MapFrom(s => s.Items.Count))
            .ForCtorParam("TotalQuantity", o => o.MapFrom(s => s.Items.Sum(i => i.Quantity)))
            .ForCtorParam("Items", o => o.MapFrom(s => s.Items));
    }
}
