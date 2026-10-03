using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class Discount : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresApproval { get; set; }
    public decimal? ApprovalThreshold { get; set; }
}
