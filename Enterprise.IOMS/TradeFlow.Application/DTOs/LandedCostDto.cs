using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;

public record LandedCostComponentDto(Guid Id, string Name, string? Type, decimal DefaultRate);
public record LandedCostAllocationDto(Guid Id, Guid? PurchaseOrderId, string? PurchaseOrderNumber,
    Guid ComponentId, string ComponentName, decimal Amount, LandedCostAllocMethod AllocationMethod);
public record CreateLandedCostComponentDto(string Name, string? Type, decimal DefaultRate);
public record CreateLandedCostAllocationDto(Guid? PurchaseOrderId, Guid ComponentId,
    decimal Amount, LandedCostAllocMethod AllocationMethod);
