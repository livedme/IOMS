namespace TradeFlow.Application.DTOs;

public record ScheduledReportDto(Guid Id, string ReportType, string? CronExpression, string? Recipients,
    string? Format, bool IsActive, DateTime? LastRunAt, string? LastRunStatus);
public record CreateScheduledReportDto(string ReportType, string? CronExpression, string? Recipients, string? Format);
