namespace TradeFlow.Domain.Entities
{
    public class StockTransferItem : BaseEntity
    {
        public Guid StockTransferId { get; set; }
        public StockTransfer StockTransfer { get; set; } = null!;
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public Guid? SourceInventoryId { get; set; }
        public Inventory? SourceInventory { get; set; }
        public Guid? TargetInventoryId { get; set; }
        public Inventory? TargetInventory { get; set; }
        public string? Notes { get; set; }
    }
}