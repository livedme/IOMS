namespace TradeFlow.Application.DTOs;

public record UserDto(string Id, string FullName, string? Email, string? Department,
    bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt, List<string> Roles);

public record FilterItem(Guid Id, string Name, bool IsSelect);
