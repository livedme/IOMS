using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Mappings;

public class PurchaseOrderDtoMappingProfile : Profile
{
    public PurchaseOrderDtoMappingProfile()
    {
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ConstructUsing((s, ctx) => new PurchaseOrderDto(
                s.Id,
                s.OrderNumber,
                s.SupplierId,
                s.Supplier != null ? s.Supplier.SupplierName : string.Empty,
                s.WarehouseId ?? Guid.Empty,
                s.Warehouse != null ? s.Warehouse.Name : string.Empty,
                s.PurchaseDate,
                s.Status,
                s.SubTotal,
                s.LabourCharge,
                s.TruckCharge,
                s.DiscountAmount,
                s.DiscountType,
                s.TotalAmount,
                s.TaxAmount,
                s.PaidAmount,
                s.DueAmount,
                s.Notes,
                s.ExpectedDeliveryDate,
                ctx.Mapper.Map<List<PurchaseOrderItemDto>>(s.Items)
            ));
    }
}
