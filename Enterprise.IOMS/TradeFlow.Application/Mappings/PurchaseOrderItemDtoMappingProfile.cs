using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Mappings;

public class PurchaseOrderItemDtoMappingProfile : Profile
{
    public PurchaseOrderItemDtoMappingProfile()
    {
        CreateMap<PurchaseOrderItem, PurchaseOrderItemDto>()
            .ConstructUsing(s => new PurchaseOrderItemDto(
                s.Id,
                s.ProductId,
                s.Product != null ? s.Product.Name : string.Empty,
                s.Product != null ? s.Product.SKU : string.Empty,
                s.Quantity,
                s.ReceivedQuantity,
                s.UnitPrice,
                s.DiscountAmount,
                s.DiscountType,
                s.TotalDiscount,
                s.LineTotal
            ));
    }
}
