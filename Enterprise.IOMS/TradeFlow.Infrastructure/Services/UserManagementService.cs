using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Constants;

namespace TradeFlow.Infrastructure.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ITenantProvider tenantProvider,
            IHttpContextAccessor httpContextAccessor,
            AuthenticationStateProvider authenticationStateProvider,
            IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _authenticationStateProvider = authenticationStateProvider;
            _dbFactory = dbFactory;
        }

        public async Task<List<UserDto>> GetUsers(string? search)
        {
            var principal = await GetPrincipalAsync();
            var isPlatformAdmin = Roles.IsPlatformAdmin(principal);

            var query = isPlatformAdmin
                ? _userManager.Users.AsQueryable()
                : _userManager.Users.Where(u => u.TenantId == _tenantProvider.GetTenantId()).AsQueryable();
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
            var principal = await GetPrincipalAsync();
            var isPlatformAdmin = Roles.IsPlatformAdmin(principal);

            var user = isPlatformAdmin
                ? await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId)
                : await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == _tenantProvider.GetTenantId());
            if (user == null) return null;
            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto(user.Id, user.FullName, user.Email, user.Department, user.IsActive, user.CreatedAt, user.LastLoginAt, roles.ToList());
        }

        public async Task ToggleUserActive(string userId)
        {
            var principal = await GetPrincipalAsync();
            var isPlatformAdmin = Roles.IsPlatformAdmin(principal);

            var user = (isPlatformAdmin
                ? await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId)
                : await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == _tenantProvider.GetTenantId()))
                ?? throw new KeyNotFoundException("User not found");
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
        }

        public async Task<List<string>> GetRoles() => await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();

        public async Task<List<string>> GetUserRoles(string userId)
        {
            var principal = await GetPrincipalAsync();
            var isPlatformAdmin = Roles.IsPlatformAdmin(principal);

            var user = (isPlatformAdmin
                ? await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId)
                : await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == _tenantProvider.GetTenantId()))
                ?? throw new KeyNotFoundException("User not found");
            return (await _userManager.GetRolesAsync(user)).ToList();
        }

        public async Task AssignRole(string userId, string role)
        {
            var principal = await GetPrincipalAsync();
            var allowedRoles = GetAllowedRoles(principal);
            if (!allowedRoles.Contains(role, StringComparer.Ordinal))
            {
                throw new UnauthorizedAccessException($"You cannot assign the '{role}' role.");
            }

            var isPlatformAdmin = Roles.IsPlatformAdmin(principal);
            var user = (isPlatformAdmin
                ? await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId)
                : await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == _tenantProvider.GetTenantId()))
                ?? throw new KeyNotFoundException("User not found");
            if (!await _roleManager.RoleExistsAsync(role)) throw new InvalidOperationException($"Role '{role}' does not exist");
            await _userManager.AddToRoleAsync(user, role);
        }

        public async Task RemoveRole(string userId, string role)
        {
            var principal = await GetPrincipalAsync();
            var allowedRoles = GetAllowedRoles(principal);
            if (!allowedRoles.Contains(role, StringComparer.Ordinal))
            {
                throw new UnauthorizedAccessException($"You cannot remove the '{role}' role.");
            }

            var isPlatformAdmin = Roles.IsPlatformAdmin(principal);
            var user = (isPlatformAdmin
                ? await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId)
                : await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == _tenantProvider.GetTenantId()))
                ?? throw new KeyNotFoundException("User not found");
            await _userManager.RemoveFromRoleAsync(user, role);
        }

        public async Task CreateRole(string roleName)
        {
            var principal = await GetPrincipalAsync();
            EnsurePlatformAdministrator(principal);

            if (await _roleManager.RoleExistsAsync(roleName)) throw new InvalidOperationException($"Role '{roleName}' already exists");
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        public async Task DeleteRole(string roleName)
        {
            var principal = await GetPrincipalAsync();
            EnsurePlatformAdministrator(principal);

            var role = await _roleManager.FindByNameAsync(roleName) ?? throw new KeyNotFoundException("Role not found");
            await _roleManager.DeleteAsync(role);
        }

        public async Task<List<string>> GetAllowedRoles()
        {
            var principal = await GetPrincipalAsync();
            return GetAllowedRoles(principal);
        }

        public async Task<List<TenantOptionDto>> GetTenants()
        {
            var principal = await GetPrincipalAsync();
            if (!Roles.IsPlatformAdmin(principal))
            {
                return [];
            }

            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Tenants
                .AsNoTracking()
                .Where(tenant => !tenant.IsDeleted)
                .OrderBy(tenant => tenant.CompanyName)
                .Select(tenant => new TenantOptionDto(tenant.Id, tenant.CompanyName, tenant.CompanyCode))
                .ToListAsync();
        }

        public async Task<UserDto> CreateUser(CreateUserDto dto)
        {
            var principal = await GetPrincipalAsync();
            var isPlatformAdmin = Roles.IsPlatformAdmin(principal);
            var allowedRoles = GetAllowedRoles(principal);

            if (allowedRoles.Count == 0)
            {
                throw new UnauthorizedAccessException("You are not allowed to create users.");
            }

            if (!allowedRoles.Contains(dto.Role, StringComparer.Ordinal))
            {
                throw new UnauthorizedAccessException($"You cannot assign the '{dto.Role}' role.");
            }

            if (!await _roleManager.RoleExistsAsync(dto.Role))
            {
                throw new InvalidOperationException($"Role '{dto.Role}' does not exist.");
            }

            Guid tenantId;
            if (isPlatformAdmin)
            {
                tenantId = dto.TenantId ?? throw new InvalidOperationException("Select a tenant for the new user.");

                await using var db = await _dbFactory.CreateDbContextAsync();
                var tenantExists = await db.Tenants.AnyAsync(tenant => tenant.Id == tenantId && !tenant.IsDeleted);
                if (!tenantExists)
                {
                    throw new InvalidOperationException("The selected tenant no longer exists.");
                }
            }
            else
            {
                tenantId = _tenantProvider.GetTenantId();
            }

            var email = dto.Email.Trim();
            if (await _userManager.FindByEmailAsync(email) is not null)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = dto.FullName.Trim(),
                Department = string.IsNullOrWhiteSpace(dto.Department) ? null : dto.Department.Trim(),
                TenantId = tenantId,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", createResult.Errors.Select(error => error.Description)));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                throw new InvalidOperationException(string.Join(" ", roleResult.Errors.Select(error => error.Description)));
            }

            return new UserDto(user.Id, user.FullName, user.Email, user.Department, user.IsActive, user.CreatedAt, user.LastLoginAt, [dto.Role]);
        }

        private async Task<ClaimsPrincipal> GetPrincipalAsync()
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            if (httpUser?.Identity?.IsAuthenticated == true)
            {
                return httpUser;
            }

            var authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            return authenticationState.User;
        }

        private static List<string> GetAllowedRoles(ClaimsPrincipal principal)
        {
            if (Roles.IsPlatformAdmin(principal))
            {
                return [.. Roles.PlatformManagedRoles];
            }

            if (Roles.IsTenantAdmin(principal))
            {
                return [.. Roles.TenantManagedRoles];
            }

            return [];
        }

        private static void EnsurePlatformAdministrator(ClaimsPrincipal principal)
        {
            if (!Roles.IsPlatformAdmin(principal))
            {
                throw new UnauthorizedAccessException("Only a System Admin or Super Admin can manage roles.");
            }
        }
    }
}
