namespace TradeFlow.Domain.Entities;

public class LandedCostComponent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public decimal DefaultRate { get; set; }
    public ICollection<LandedCostAllocation> Allocations { get; set; } = new List<LandedCostAllocation>();
}
