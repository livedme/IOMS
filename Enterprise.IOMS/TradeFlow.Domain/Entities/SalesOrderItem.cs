using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class SalesOrderItem : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public int ShippedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal TotalDiscount { get; set; }    
    public decimal LineTotalPrice { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure? UoM { get; set; }

    // Partial shipment and return support
    public int ReturnedQuantity { get; set; }
    public bool IsBackOrdered { get; set; }
}
