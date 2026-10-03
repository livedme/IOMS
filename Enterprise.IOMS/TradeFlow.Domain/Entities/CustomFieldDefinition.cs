using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities;

public class CustomFieldDefinition : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public CustomFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public string? Options { get; set; }
    public int SortOrder { get; set; }
    public ICollection<CustomFieldValue> Values { get; set; } = new List<CustomFieldValue>();
}
