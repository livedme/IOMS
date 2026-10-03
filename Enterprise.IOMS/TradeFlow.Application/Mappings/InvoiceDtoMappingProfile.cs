using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class InvoiceDtoMappingProfile : Profile
{
    public InvoiceDtoMappingProfile()
    {
        CreateMap<Invoice, InvoiceDto>()
            .ForCtorParam("CustomerName", o => o.MapFrom(s => s.Customer != null ? s.Customer.CustomerName : null))
            .ForCtorParam("SupplierName", o => o.MapFrom(s => s.Supplier != null ? s.Supplier.SupplierName : null))
            .ForCtorParam("BalanceDue", o => o.MapFrom(s => s.TotalAmount - s.PaidAmount));
    }
}
