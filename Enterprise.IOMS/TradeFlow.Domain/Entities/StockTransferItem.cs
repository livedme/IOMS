namespace TradeFlow.Domain.Entities
{
    public class StockTransferItem : BaseEntity
    {
        public Guid StockTransferId { get; set; }
        public StockTransfer StockTransfer { get; set; } = null!;
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public Guid? FromInventoryId { get; set; }
        public Inventory? FromInventory { get; set; }
        public Guid? ToInventoryId { get; set; }
        public Inventory? ToInventory { get; set; }
        public string? Notes { get; set; }
    }
}