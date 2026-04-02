using IOMS.Domain.Enums;

namespace IOMS.Domain.Entities;

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

public class Stocktake : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public StocktakeStatus Status { get; set; } = StocktakeStatus.Draft;
    public string? Notes { get; set; }
    public ICollection<StocktakeItem> Items { get; set; } = new List<StocktakeItem>();
}

public class StocktakeItem : BaseEntity
{
    public Guid StocktakeId { get; set; }
    public Stocktake Stocktake { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int SystemQuantity { get; set; }
    public int? CountedQuantity { get; set; }
    public int Variance => (CountedQuantity ?? 0) - SystemQuantity;
    public string? Notes { get; set; }
}

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

public class PurchaseReturnItem : BaseEntity
{
    public Guid PurchaseReturnId { get; set; }
    public PurchaseReturn PurchaseReturn { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

public class Kit : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<KitComponent> Components { get; set; } = new List<KitComponent>();
}

public class KitComponent : BaseEntity
{
    public Guid KitId { get; set; }
    public Kit Kit { get; set; } = null!;
    public Guid ComponentProductId { get; set; }
    public Product ComponentProduct { get; set; } = null!;
    public int Quantity { get; set; }
}

public class LandedCostComponent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public decimal DefaultRate { get; set; }
    public ICollection<LandedCostAllocation> Allocations { get; set; } = new List<LandedCostAllocation>();
}

public class LandedCostAllocation : BaseEntity
{
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid ComponentId { get; set; }
    public LandedCostComponent Component { get; set; } = null!;
    public decimal Amount { get; set; }
    public LandedCostAllocMethod AllocationMethod { get; set; }
}
