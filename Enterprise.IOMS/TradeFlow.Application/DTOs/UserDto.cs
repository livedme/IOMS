namespace TradeFlow.Application.DTOs;

public record UserDto(string Id, string FullName, string? Email, string? Department,
    bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt, List<string> Roles);

public record CreateUserDto(
    string FullName,
    string Email,
    string Password,
    string? Department,
    string Role,
    Guid? TenantId);

public record TenantOptionDto(Guid Id, string CompanyName, string CompanyCode);

public record FilterItem(Guid Id, string Name, bool IsSelect);
