using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class TaxJurisdictionDtoMappingProfile : Profile
{
    public TaxJurisdictionDtoMappingProfile()
    {
        CreateMap<TaxJurisdiction, TaxJurisdictionDto>();
    }
}
