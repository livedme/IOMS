namespace TradeFlow.Domain.Entities;

public class PriceListItem : BaseEntity
{
    public Guid PriceListId { get; set; }
    public PriceList PriceList { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int? MinQuantity { get; set; }
}
