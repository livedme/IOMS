namespace TradeFlow.Domain.Entities;

public class TenantSetting : BaseEntity
{
    /// <summary>
    /// Setting key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Setting value (can be JSON)
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Description of this setting
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a system-level setting (read-only)
    /// </summary>
    public bool IsSystem { get; set; } = false;

}
