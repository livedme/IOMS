using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class LandedCostAllocation : BaseEntity
{
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid ComponentId { get; set; }
    public LandedCostComponent Component { get; set; } = null!;
    public decimal Amount { get; set; }
    public LandedCostAllocMethod AllocationMethod { get; set; }
}
