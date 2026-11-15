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

    /// <summary>Roles the signed-in caller is allowed to assign when creating a user.</summary>
    Task<List<string>> GetAllowedRoles();

    /// <summary>Tenants a platform administrator can create users for. Empty for tenant admins.</summary>
    Task<List<TenantOptionDto>> GetTenants();

    /// <summary>Creates a user in the caller's tenant (or the selected tenant for platform admins).</summary>
    Task<UserDto> CreateUser(CreateUserDto dto);
}
