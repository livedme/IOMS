using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Mappings;

public class SalesOrderItemDtoMappingProfile : Profile
{
    public SalesOrderItemDtoMappingProfile()
    {
        CreateMap<SalesOrderItem, SalesOrderItemDto>()
            .ConstructUsing(s => new SalesOrderItemDto(
                s.Id,
                s.ProductId,
                s.Product != null ? s.Product.Name : string.Empty,
                s.Product != null ? s.Product.SKU : string.Empty,
                s.Quantity,
                s.ShippedQuantity,
                s.UnitPrice,
                s.DiscountAmount,
                s.DiscountType,
                s.TotalDiscount,
                s.LineTotalPrice));
    }
}
