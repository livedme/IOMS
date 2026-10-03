namespace TradeFlow.Domain.Entities;

public class DeliveryNote : BaseEntity
{
    public string DeliveryNoteNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? ShippedBy { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Notes { get; set; }
}
