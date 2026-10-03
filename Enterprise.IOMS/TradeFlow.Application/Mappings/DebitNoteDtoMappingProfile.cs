using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class DebitNoteDtoMappingProfile : Profile
{
    public DebitNoteDtoMappingProfile()
    {
        CreateMap<DebitNote, DebitNoteDto>()
            .ForCtorParam("InvoiceNumber", o => o.MapFrom(s => s.Invoice != null ? s.Invoice.InvoiceNumber : string.Empty));
    }
}
