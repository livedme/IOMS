namespace TradeFlow.Domain.Entities;

public class ScheduledReport : BaseEntity
{
    public string ReportType { get; set; } = string.Empty;
    public string? CronExpression { get; set; }
    public string? Recipients { get; set; }
    public string? Format { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public string? LastRunStatus { get; set; }
}
