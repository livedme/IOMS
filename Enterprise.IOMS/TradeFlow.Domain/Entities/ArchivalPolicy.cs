namespace TradeFlow.Domain.Entities;

public class ArchivalPolicy : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
}
