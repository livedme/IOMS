using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class PurchaseReturn : BaseEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public PurchaseReturnStatus Status { get; set; } = PurchaseReturnStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
}
