using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal LabourCharge { get; set; }
    public decimal TruckCharge { get; set; }
    public decimal DiscountAmount { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }
    public decimal ExchangeRate { get; set; } = 1;
    public string? Notes { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<PurchaseReturn> Returns { get; set; } = new List<PurchaseReturn>();
}
