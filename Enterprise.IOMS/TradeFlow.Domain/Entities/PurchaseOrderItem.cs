using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class PurchaseOrderItem : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal TotalDiscount { get; set; }    
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure? UoM { get; set; }
}
