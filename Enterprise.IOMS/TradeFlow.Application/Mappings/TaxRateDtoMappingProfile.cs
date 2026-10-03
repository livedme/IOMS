using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class TaxRateDtoMappingProfile : Profile
{
    public TaxRateDtoMappingProfile()
    {
        CreateMap<TaxRate, TaxRateDto>()
            .ForCtorParam("JurisdictionName", o => o.MapFrom(s => s.TaxJurisdiction != null ? s.TaxJurisdiction.Name : null));
    }
}
