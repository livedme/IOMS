using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class CurrencyDtoMappingProfile : Profile
{
    public CurrencyDtoMappingProfile()
    {
        CreateMap<Currency, CurrencyDto>();
    }
}
