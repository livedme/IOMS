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
            .ForMember(d => d.ItemsLine, o => o.MapFrom(s => s.Items));
    }
}
