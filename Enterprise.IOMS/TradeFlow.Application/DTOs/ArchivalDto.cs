namespace TradeFlow.Application.DTOs;

public record ArchivalPolicyDto(Guid Id, string EntityType, int RetentionDays, bool IsActive, DateTime? LastRunAt);
public record CreateArchivalPolicyDto(string EntityType, int RetentionDays);
