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
            .ForCtorParam("SourceWarehouseName", o => o.MapFrom(s => s.SourceInventory != null && s.SourceInventory.Warehouse != null ? s.SourceInventory.Warehouse.Name : string.Empty));

        CreateMap<StockTransfer, StockTransferListDto>()
            .ForCtorParam("SourceWarehouseName", o => o.MapFrom(s => s.SourceWarehouse != null ? s.SourceWarehouse.Name : string.Empty))
            .ForCtorParam("TargetWarehouseName", o => o.MapFrom(s => s.TargetWarehouse != null ? s.TargetWarehouse.Name : string.Empty))
            .ForCtorParam("TotalLines", o => o.MapFrom(s => s.Items.Count))
            .ForCtorParam("TotalQuantity", o => o.MapFrom(s => s.Items.Sum(i => i.Quantity)))
            .ForCtorParam("Items", o => o.MapFrom(s => s.Items));
    }
}
