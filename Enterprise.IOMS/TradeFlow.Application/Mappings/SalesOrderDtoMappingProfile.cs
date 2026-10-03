using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Mappings;

public class SalesOrderDtoMappingProfile : Profile
{
    public SalesOrderDtoMappingProfile()
    {
        CreateMap<SalesOrder, SalesOrderDto>()
            .ConstructUsing((s, ctx) => new SalesOrderDto(
                s.Id,
                s.OrderNumber,
                s.CustomerId,
                ctx.Mapper.Map<CustomerDto>(s.Customer),
                s.BranchId,
                s.Branch != null
                    ? new BranchDto(s.BranchId ?? Guid.Empty, s.Branch.Name, s.Branch.Code, s.Branch.Location, s.Branch.Address, s.Branch.IsActive)
                    : null,
                s.OrderDate,
                s.Status,
                s.Naration,
                s.Chalan,
                s.SubTotal,
                s.TruckCharge,
                s.LabourCharge,
                s.TaxAmount,
                s.DiscountAmount,
                s.DiscountType,
                s.TotalAmount,
                s.PaidAmount,
                s.DueAmount,
                s.Notes,
                s.ExchangeRate,
                s.ExpectedDeliveryDate,
                ctx.Mapper.Map<List<SalesOrderItemDto>>(s.Items),
                s.Items.Count));
    }
}
