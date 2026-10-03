using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IUserManagementService
{
    Task<List<UserDto>> GetUsers(string? search);
    Task<UserDto?> GetUserById(string userId);
    Task ToggleUserActive(string userId);
    Task<List<string>> GetRoles();
    Task<List<string>> GetUserRoles(string userId);
    Task AssignRole(string userId, string role);
    Task RemoveRole(string userId, string role);
    Task CreateRole(string roleName);
    Task DeleteRole(string roleName);
}
