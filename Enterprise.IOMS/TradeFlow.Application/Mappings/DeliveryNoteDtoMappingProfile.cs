using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class DeliveryNoteDtoMappingProfile : Profile
{
    public DeliveryNoteDtoMappingProfile()
    {
        CreateMap<DeliveryNote, DeliveryNoteDto>()
            .ForCtorParam("SalesOrderNumber", o => o.MapFrom(s => s.SalesOrder != null ? s.SalesOrder.OrderNumber : string.Empty));
    }
}
