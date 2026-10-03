using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class ShipmentDtoMappingProfile : Profile
{
    public ShipmentDtoMappingProfile()
    {
        CreateMap<Shipment, ShipmentDto>()
            .ForCtorParam("DeliveryNoteId", o => o.MapFrom(_ => (Guid?)null))
            .ForCtorParam("Carrier", o => o.MapFrom(s => s.CarrierName))
            .ForCtorParam("ShippedDate", o => o.MapFrom(s => s.ShipDate))
            .ForCtorParam("DeliveredDate", o => o.MapFrom(s => s.ActualDelivery));
    }
}
