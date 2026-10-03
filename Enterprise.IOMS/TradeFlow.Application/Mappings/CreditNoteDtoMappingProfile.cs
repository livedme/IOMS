using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class CreditNoteDtoMappingProfile : Profile
{
    public CreditNoteDtoMappingProfile()
    {
        CreateMap<CreditNote, CreditNoteDto>()
            .ForCtorParam("InvoiceNumber", o => o.MapFrom(s => s.Invoice != null ? s.Invoice.InvoiceNumber : string.Empty));
    }
}
