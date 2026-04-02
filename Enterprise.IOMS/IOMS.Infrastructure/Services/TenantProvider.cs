using System.Security.Claims;
using IOMS.Infrastructure.Data;
using IOMS.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace IOMS.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetTenantId()
    {
        var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
        return tenantClaim != null ? Guid.Parse(tenantClaim) : Guid.Parse(AppConstants.DefaultTenantId);
    }

    public string? GetUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
