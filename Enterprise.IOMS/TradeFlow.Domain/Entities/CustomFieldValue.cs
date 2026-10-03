namespace TradeFlow.Domain.Entities;

public class CustomFieldValue : BaseEntity
{
    public Guid DefinitionId { get; set; }
    public CustomFieldDefinition Definition { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string? Value { get; set; }
}
