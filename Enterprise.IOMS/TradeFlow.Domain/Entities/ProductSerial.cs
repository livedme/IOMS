using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class ProductSerial : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string SerialNumber { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? QRCode { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public ProductSerialStatus Status { get; set; } = ProductSerialStatus.Available;
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public string? BinLocation { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
}
