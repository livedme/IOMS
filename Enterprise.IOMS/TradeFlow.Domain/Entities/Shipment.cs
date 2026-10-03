using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class Shipment : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? ShipDate { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public DateTime? ActualDelivery { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
    public decimal FreightCost { get; set; }
    public string? Notes { get; set; }
}
