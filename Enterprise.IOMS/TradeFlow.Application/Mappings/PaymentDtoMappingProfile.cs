using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Mappings;

public class PaymentDtoMappingProfile : Profile
{
    public PaymentDtoMappingProfile()
    {
        CreateMap<Payment, PaymentDto>()
            .ForCtorParam("Method", o => o.MapFrom(s => s.PaymentMethod))
            .ForCtorParam("InvoiceNumber", o => o.MapFrom(s => s.Invoice != null ? s.Invoice.InvoiceNumber : null));
    }
}
