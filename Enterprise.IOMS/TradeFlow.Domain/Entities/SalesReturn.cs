using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class SalesReturn : BaseEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public SalesReturnStatus Status { get; set; } = SalesReturnStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public ICollection<SalesReturnItem> Items { get; set; } = new List<SalesReturnItem>();
}
