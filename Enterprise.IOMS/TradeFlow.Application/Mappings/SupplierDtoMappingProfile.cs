using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class SupplierDtoMappingProfile : Profile
{
    public SupplierDtoMappingProfile()
    {
        CreateMap<Supplier, SupplierDto>();
    }
}
