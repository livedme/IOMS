using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TradeFlow.Infrastructure.Data;

namespace TradeFlow.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantContext _tenantContext;
    private readonly ILogger<TenantProvider> _logger;

    public TenantProvider(
        IHttpContextAccessor httpContextAccessor,
        TenantContext tenantContext,
        ILogger<TenantProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Resolves the caller's tenant. Fails closed: a missing or malformed claim throws instead of
    /// silently falling back to a default tenant, which would otherwise serve one customer's data
    /// to another.
    /// </summary>
    /// <remarks>
    /// Two resolution paths are needed. During a live HTTP request (static server rendering,
    /// Identity endpoints, API calls) the ambient <c>HttpContext</c> carries the principal. During
    /// a Blazor Server circuit event the <c>HttpContext</c> is <c>null</c>, so the circuit-scoped
    /// <see cref="TenantContext"/> is authoritative instead.
    /// </remarks>
    public Guid GetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is not null)
        {
            var principal = httpContext.User;

            if (principal.Identity?.IsAuthenticated != true)
            {
                _logger.LogError(
                    "Tenant resolution requested for an unauthenticated principal (TraceId {TraceId}).",
                    httpContext.TraceIdentifier);
                throw new InvalidOperationException("Tenant cannot be resolved for an unauthenticated request.");
            }

            var tenantId = TryResolveFromPrincipal(principal);
            if (tenantId is not { } resolved)
            {
                throw new InvalidOperationException("Authenticated user has no valid tenant identifier.");
            }

            return resolved;
        }

        // Blazor Server circuit event: no ambient HttpContext.
        if (_tenantContext.TenantId is { } circuitTenant)
        {
            return circuitTenant;
        }

        _logger.LogError(
            "Tenant resolution requested outside an HTTP request and before the circuit tenant was initialised. " +
            "The tenant must be initialised from the authentication state before data access.");
        throw new InvalidOperationException("Tenant cannot be resolved: no ambient request and no initialised circuit tenant.");
    }

    /// <summary>
    /// Non-throwing variant for paths that must degrade rather than fail, such as cache-key
    /// construction. Returns <c>null</c> when no tenant is resolvable, letting callers skip caching
    /// instead of risking a cross-tenant key collision.
    /// </summary>
    public Guid? TryGetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return TryResolveFromPrincipal(httpContext.User);
        }

        return _tenantContext.TenantId;
    }

    public string? GetUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId ?? _tenantContext.UserId;
    }

    private Guid? TryResolveFromPrincipal(ClaimsPrincipal principal)
    {
        var tenantClaim = principal.FindFirst("TenantId")?.Value;

        if (string.IsNullOrWhiteSpace(tenantClaim))
        {
            _logger.LogError("Authenticated principal has no TenantId claim (Subject {Subject}).", principal.Identity?.Name);
            return null;
        }

        if (!Guid.TryParse(tenantClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            _logger.LogError("TenantId claim '{TenantClaim}' is not a valid tenant identifier.", tenantClaim);
            return null;
        }

        // Cache so later circuit events, which have no HttpContext, can still resolve the tenant.
        _tenantContext.Set(tenantId, principal.FindFirstValue(ClaimTypes.NameIdentifier));
        return tenantId;
    }
}
