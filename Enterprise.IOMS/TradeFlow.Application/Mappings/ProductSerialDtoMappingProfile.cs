using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class ProductSerialDtoMappingProfile : Profile
{
    public ProductSerialDtoMappingProfile()
    {
        CreateMap<ProductSerial, ProductSerialDto>()
            .ForCtorParam("ProductName", o => o.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty))
            .ForCtorParam("WarehouseName", o => o.MapFrom(s => s.Warehouse != null ? s.Warehouse.Name : null))
            .ForCtorParam("SupplierName", o => o.MapFrom(s => s.Supplier != null ? s.Supplier.SupplierName : null));
    }
}
