using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities
{
    public class StockTransfer : BaseEntity
    {
        public string TransferNumber { get; set; } = string.Empty;
        public DateTime TransferDate { get; set; } = DateTime.UtcNow;
        public Guid FromWarehouseId { get; set; }
        public Warehouse FromWarehouse { get; set; } = null!;
        public Guid ToWarehouseId { get; set; }
        public Warehouse ToWarehouse { get; set; } = null!;
        public StockTransferStatus Status { get; set; } = StockTransferStatus.Completed;
        public string? Notes { get; set; }
        public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();

        public int TotalLines => Items.Count;
        public int TotalQuantity => Items.Sum(i => i.Quantity);
    }
}