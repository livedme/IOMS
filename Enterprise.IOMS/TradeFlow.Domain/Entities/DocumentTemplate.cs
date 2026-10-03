namespace TradeFlow.Domain.Entities;

public class DocumentTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
