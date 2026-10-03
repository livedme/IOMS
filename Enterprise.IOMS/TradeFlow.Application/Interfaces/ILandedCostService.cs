using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface ILandedCostService
{
    Task<List<LandedCostComponentDto>> GetComponents();
    Task<Guid> CreateComponent(CreateLandedCostComponentDto dto);
    Task<List<LandedCostAllocationDto>> GetAllocations(Guid? purchaseOrderId);
    Task<Guid> AllocateCost(CreateLandedCostAllocationDto dto);
}
