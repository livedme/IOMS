using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Application.Mappings;

public class SalesReturnDtoMappingProfile : Profile
{
    public SalesReturnDtoMappingProfile()
    {
        CreateMap<SalesReturn, SalesReturnDto>()
            .ForMember(d => d.SalesOrderNumber, o => o.MapFrom(s => s.SalesOrder.OrderNumber))
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.SalesOrder.Customer != null ? s.SalesOrder.Customer.CustomerName : string.Empty))
            .ForMember(d => d.CustomerPhone, o => o.MapFrom(s => s.SalesOrder.Customer != null ? s.SalesOrder.Customer.CustomerPhone : null))
            .ForMember(d => d.CustomerCity, o => o.MapFrom(s => s.SalesOrder.Customer != null ? s.SalesOrder.Customer.City : null))
            .ForMember(d => d.CustomerState, o => o.MapFrom(s => s.SalesOrder.Customer != null ? s.SalesOrder.Customer.State : null))
            .ForMember(d => d.ItemsLine, o => o.MapFrom(s => s.Items));
    }
}
