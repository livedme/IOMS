namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Holds the tenant resolved for the current request or Blazor circuit.
/// </summary>
/// <remarks>
/// <para>
/// This exists because <see cref="IHttpContextAccessor"/> is only populated while an HTTP
/// request is in flight. Once a Blazor Server circuit is established, the initial request has
/// completed and <c>HttpContext</c> is <c>null</c> for every subsequent event (button click,
/// form post, <c>OnInitializedAsync</c> re-run). Resolving the tenant from the ambient
/// <c>HttpContext</c> alone therefore fails for all interactive pages.
/// </para>
/// <para>
/// The circuit is scoped to a single signed-in user on a single tab, and a user cannot change
/// tenant mid-circuit, so caching the tenant for the lifetime of the scope is safe. The web layer
/// populates this type from the Blazor <c>AuthenticationStateProvider</c>; see
/// <c>TenantContextInitializer.razor</c>.
/// </para>
/// </remarks>
public sealed class TenantContext
{
    private Guid? _tenantId;
    private string? _userId;

    /// <summary>The resolved tenant, or <c>null</c> if no tenant has been resolved yet.</summary>
    public Guid? TenantId => _tenantId;

    /// <summary>The resolved user id, or <c>null</c> if no user has been resolved yet.</summary>
    public string? UserId => _userId;

    /// <summary>True once a valid tenant has been recorded.</summary>
    public bool IsResolved => _tenantId.HasValue;

    /// <summary>
    /// Records the tenant for this scope. A missing or empty tenant is ignored rather than stored,
    /// so a failed resolution can never be mistaken for a successful one.
    /// </summary>
    public void Set(Guid tenantId, string? userId)
    {
        if (tenantId == Guid.Empty)
        {
            return;
        }

        _tenantId = tenantId;
        _userId = userId;
    }
}
