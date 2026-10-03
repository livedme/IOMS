using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class ExchangeRateDtoMappingProfile : Profile
{
    public ExchangeRateDtoMappingProfile()
    {
        CreateMap<ExchangeRate, ExchangeRateDto>()
            .ForMember(d => d.FromCurrencyCode, o => o.MapFrom(s => s.FromCurrency.Code))
            .ForMember(d => d.ToCurrencyCode, o => o.MapFrom(s => s.ToCurrency.Code));
    }
}
