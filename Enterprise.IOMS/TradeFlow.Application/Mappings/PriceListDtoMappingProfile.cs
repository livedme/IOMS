using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class PriceListDtoMappingProfile : Profile
{
    public PriceListDtoMappingProfile()
    {
        CreateMap<PriceList, PriceListDto>()
            .ForMember(d => d.CurrencyCode, o => o.MapFrom(s => s.Currency != null ? s.Currency.Code : null));
    }
}
