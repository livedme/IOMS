namespace TradeFlow.Application.DTOs;

public record AuditLogDto(Guid Id, string TableName, Guid RecordId, string Action,
    string? OldValues, string? NewValues, string? UserId, DateTime Timestamp);
