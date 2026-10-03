using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class JournalEntryLineDtoMappingProfile : Profile
{
    public JournalEntryLineDtoMappingProfile()
    {
        CreateMap<JournalEntryLine, JournalEntryLineDto>()
            .ForMember(d => d.AccountCode, o => o.MapFrom(s => s.Account.Code))
            .ForMember(d => d.AccountName, o => o.MapFrom(s => s.Account.Name));
    }
}
