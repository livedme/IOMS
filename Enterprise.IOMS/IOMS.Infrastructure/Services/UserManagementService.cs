using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace IOMS.Infrastructure.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITenantProvider _tenantProvider;

        public UserManagementService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ITenantProvider tenantProvider)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantProvider = tenantProvider;
        }

        public async Task<List<UserDto>> GetUsers(string? search)
        {
            var tenantId = _tenantProvider.GetTenantId();
            var query = _userManager.Users.Where(u => u.TenantId == tenantId).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));

            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            var result = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserDto(user.Id, user.FullName, user.Email, user.Department, user.IsActive, user.CreatedAt, user.LastLoginAt, roles.ToList()));
            }
            return result;
        }

        public async Task<UserDto?> GetUserById(string userId)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == _tenantProvider.GetTenantId());
            if (user == null) return null;
            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto(user.Id, user.FullName, user.Email, user.Department, user.IsActive, user.CreatedAt, user.LastLoginAt, roles.ToList());
        }

        public async Task ToggleUserActive(string userId)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == _tenantProvider.GetTenantId())
                ?? throw new KeyNotFoundException("User not found");
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
        }

        public async Task<List<string>> GetRoles() => await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();

        public async Task<List<string>> GetUserRoles(string userId)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == _tenantProvider.GetTenantId())
                ?? throw new KeyNotFoundException("User not found");
            return (await _userManager.GetRolesAsync(user)).ToList();
        }

        public async Task AssignRole(string userId, string role)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == _tenantProvider.GetTenantId())
                ?? throw new KeyNotFoundException("User not found");
            if (!await _roleManager.RoleExistsAsync(role)) throw new InvalidOperationException($"Role '{role}' does not exist");
            await _userManager.AddToRoleAsync(user, role);
        }

        public async Task RemoveRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException("User not found");
            await _userManager.RemoveFromRoleAsync(user, role);
        }

        public async Task CreateRole(string roleName)
        {
            if (await _roleManager.RoleExistsAsync(roleName)) throw new InvalidOperationException($"Role '{roleName}' already exists");
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        public async Task DeleteRole(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName) ?? throw new KeyNotFoundException("Role not found");
            await _roleManager.DeleteAsync(role);
        }
    }
}
