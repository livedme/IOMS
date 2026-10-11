using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Interfaces;

namespace TradeFlow.Web.Components.Shared.Dashboard;

/// <summary>
/// Base for a self-loading dashboard tile. Each card owns the read that produces its own data rather
/// than the page loading every tile up front and pushing the result in as a parameter. That keeps the
/// load logic beside the markup that renders it, and lets a slow read hold up only its own card: the
/// children's parameter sets run concurrently, so the tiles fill in as each read returns.
/// </summary>
/// <typeparam name="TData">Shape of the read model the tile renders.</typeparam>
public abstract class DashboardTileBase<TData> : ComponentBase
{
    [Inject] protected IDashboardService DashboardService { get; set; } = default!;

    [Inject] protected ILogger<DashboardTileBase<TData>> Logger { get; set; } = default!;

    /// <summary>
    /// Period currently applied by the dashboard filter strip. Range-scoped tiles read
    /// <see cref="DashboardRange.Days"/> from it; the header caption reads its label.
    /// </summary>
    [Parameter]
    public DashboardRange Range { get; set; } = DashboardRanges.Resolve(DashboardRanges.DefaultLabel, DateTime.UtcNow);

    /// <summary>
    /// Opaque value the page bumps to force every tile to reload (toolbar refresh, range change). A
    /// tile reloads only when this changes, so an unrelated parent render — such as a store-filter
    /// change, which does not affect the tenant-wide reads — does not trigger a re-query.
    /// </summary>
    [Parameter]
    public int ReloadToken { get; set; }

    /// <summary>The read model rendered by this tile.</summary>
    protected TData Data { get; private set; } = default!;

    /// <summary>True until the current read completes, so the tile can show its own spinner.</summary>
    protected bool Loading { get; private set; } = true;

    /// <summary>Value rendered before the first read returns and when a read fails.</summary>
    protected abstract TData EmptyData { get; }

    /// <summary>Reads this tile's data for the current <see cref="Range"/>.</summary>
    protected abstract Task<TData> LoadAsync();

    private int _loadedToken = int.MinValue;
    private int _loadedDays = int.MinValue;

    protected override async Task OnParametersSetAsync()
    {
        // Reload only when the window or the explicit reload token actually changed. This is what
        // stops every tile re-querying on each parent render.
        if (_loadedToken == ReloadToken && _loadedDays == Range.Days) return;

        _loadedToken = ReloadToken;
        _loadedDays = Range.Days;

        Loading = true;
        Data = EmptyData;

        try
        {
            Data = await LoadAsync();
        }
        catch (Exception ex)
        {
            // Substitute the empty value so one failed read degrades to an empty tile rather than
            // taking the whole dashboard down. The failure is logged at Error with the exception, so
            // an outage is distinguishable in the log from a genuinely empty dashboard.
            Logger.LogError(ex, "Dashboard tile '{DashboardTile}' failed; rendering an empty tile.", GetType().Name);
            Data = EmptyData;
        }
        finally
        {
            Loading = false;
        }
    }
}
