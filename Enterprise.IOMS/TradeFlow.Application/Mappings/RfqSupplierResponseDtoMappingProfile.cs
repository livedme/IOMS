using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class RfqSupplierResponseDtoMappingProfile : Profile
{
    public RfqSupplierResponseDtoMappingProfile()
    {
        CreateMap<RfqSupplierResponse, RfqSupplierResponseDto>()
            .ForCtorParam("SupplierName", o => o.MapFrom(s => s.Supplier != null ? s.Supplier.SupplierName : string.Empty));
    }
}
