using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class SalesOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string Naration { get; set; } = string.Empty;
    public string Chalan { get; set; } = string.Empty;    
    public decimal SubTotal { get; set; }
    public decimal LabourCharge { get; set; }
    public decimal TruckCharge { get; set;}
    public decimal TaxAmount { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountAmount { get; set; }    
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public decimal ExchangeRate { get; set; } = 1;
    public string? Notes { get; set; }
    public string? ShippingAddress { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<DeliveryNote> DeliveryNotes { get; set; } = new List<DeliveryNote>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    // Order processing enhancements
    public bool IsBackOrder { get; set; }
    public string? ShippingCarrier { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? DeliveredDate { get; set; }    
}
