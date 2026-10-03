using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class SalesQuoteDtoMappingProfile : Profile
{
    public SalesQuoteDtoMappingProfile()
    {
        CreateMap<SalesQuote, SalesQuoteDto>()
            .ForCtorParam("CustomerName", o => o.MapFrom(s => s.Customer != null ? s.Customer.CustomerName : string.Empty));
    }
}
