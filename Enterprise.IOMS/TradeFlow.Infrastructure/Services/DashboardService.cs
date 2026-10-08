using System.Diagnostics;
using System.Text;
using AutoMapper;
using Azure.Core;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace TradeFlow.Infrastructure.Services;

// ─── Dashboard Service ──────────────────────────────────────────────────────
public class DashboardService : IDashboardService
{
    /// <summary>Cache key prefix for every dashboard-derived read model.</summary>
    public const string CachePrefix = "dashboard:";

    private static readonly TimeSpan KpiLifetime = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SnapshotLifetime = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan SlowQueryThreshold = TimeSpan.FromMilliseconds(400);

    /// <summary>
    /// Longest overview window any dashboard range preset can ask for. A "Last 12 Months" window
    /// reaches 366 days when it spans a leap day, so the bound has to allow for that.
    /// </summary>
    private const int MaxOverviewDays = 366;

    /// <summary>
    /// Windows wider than this are plotted as monthly buckets instead of daily ones. At daily
    /// granularity a one-year window is 365 near-identical samples that render as a solid block with
    /// an x-axis whose "Oct 02" and "Oct 01" ends cannot be told apart; monthly buckets show the same
    /// trend in a readable number of points. Totals are unaffected — only the grouping changes.
    /// </summary>
    private const int MonthlyBucketThresholdDays = 62;

    private readonly ApplicationDbContext _context;
    private readonly ITenantCache _cache;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApplicationDbContext context,
        ITenantCache cache,
        ITenantProvider tenantProvider,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _cache = cache;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public Task<DashboardKpiDto> GetDashboardKPIs() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}kpis",
            KpiLifetime,
            ct => BuildDashboardKpisAsync(ct));

    private async Task<DashboardKpiDto> BuildDashboardKpisAsync(CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // These are twelve independent aggregates over different tables. They are issued
        // sequentially rather than through Task.WhenAll: EF Core's DbContext is not thread-safe
        // and throws "A second operation was started on this context instance before a previous
        // operation completed" if two commands overlap on one instance. Fanning them out would
        // need a separate DbContext per query (IDbContextFactory).
        //
        // The result is cached for KpiLifetime, so this only runs on a cache miss, and the
        // remaining latency is dominated by the round trips rather than by the aggregation.
        var totalProducts = await _context.Products.CountAsync(ct);
        var activeCustomers = await _context.Customers.CountAsync(c => c.IsActive, ct);
        var activeSuppliers = await _context.Suppliers.CountAsync(s => s.IsActive, ct);

        var salesThisMonth = await _context.SalesOrders
            .Where(o => o.OrderDate >= startOfMonth && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount, ct);

        var purchasesThisMonth = await _context.PurchaseOrders
            .Where(p => p.PurchaseDate >= startOfMonth && p.Status != PurchaseOrderStatus.Cancelled)
            .SumAsync(p => p.TotalAmount, ct);

        var pendingSales = await _context.SalesOrders.CountAsync(o => o.Status == OrderStatus.Pending, ct);
        var pendingPurchases = await _context.PurchaseOrders
            .CountAsync(p => p.Status == PurchaseOrderStatus.Draft || p.Status == PurchaseOrderStatus.Submitted, ct);

        // Join through Inventory -> Product rather than Include, so the reorder level and the
        // cost price are read in the same scan instead of pulling full entities into memory.
        var lowStock = await _context.Inventories
            .Where(i => i.Product.ReorderStockLevel > 0 && i.Quantity <= i.Product.ReorderStockLevel)
            .CountAsync(ct);

        var arBalance = await _context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Sales && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => i.TotalAmount - i.PaidAmount, ct);

        var apBalance = await _context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => i.TotalAmount - i.PaidAmount, ct);

        var overdueInvoices = await _context.Invoices
            .CountAsync(i => i.DueDate < now && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled, ct);

        var inventoryValue = await _context.Inventories
            .SumAsync(i => (decimal?)(i.Quantity * i.Product.CostPrice), ct);

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (elapsedMs > SlowQueryThreshold.TotalMilliseconds)
        {
            _logger.LogWarning(
                "GetDashboardKPIs issued {QueryCount} queries in {ElapsedMs:F0} ms; consider consolidating the aggregates.",
                12,
                elapsedMs);
        }

        return new DashboardKpiDto(
            totalProducts, activeCustomers, activeSuppliers,
            salesThisMonth, purchasesThisMonth,
            pendingSales, pendingPurchases, lowStock,
            arBalance, apBalance, overdueInvoices, inventoryValue ?? 0m);
    }

    public async Task<List<MonthlySalesDto>> GetMonthlySalesData(int months)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months);
        var data = await _context.SalesOrders
            .Where(o => o.OrderDate >= cutoff && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(o => o.TotalAmount),
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        return data.Select(d => new MonthlySalesDto(
            $"{d.Year}-{d.Month:D2}", d.Amount, d.Count)).ToList();
    }

    public async Task<List<MonthlyPurchaseDto>> GetMonthlyPurchaseData(int months)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months);
        var data = await _context.PurchaseOrders
            .Where(p => p.PurchaseDate >= cutoff && p.Status != PurchaseOrderStatus.Cancelled)
            .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(p => p.TotalAmount), Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        return data.Select(d => new MonthlyPurchaseDto($"{d.Year}-{d.Month:D2}", d.Amount, d.Count)).ToList();
    }

    public Task<List<TopProductDto>> GetTopProducts(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}top-products:{count}",
            SnapshotLifetime,
            async ct =>
            {
                var data = await _context.SalesOrderItems
                    .Where(i => i.SalesOrder.Status != OrderStatus.Cancelled)
                    .GroupBy(i => new { i.ProductId, i.Product.Name })
                    .Select(g => new { g.Key.Name, Revenue = g.Sum(i => i.Quantity * i.UnitPrice), Units = g.Sum(i => i.Quantity) })
                    .OrderByDescending(x => x.Revenue)
                    .Take(count)
                    .ToListAsync(ct);

                return data.Select(d => new TopProductDto(d.Name, d.Revenue, d.Units)).ToList();
            });

    public async Task<List<LowStockAlertDto>> GetLowStockAlerts(int count)
    {
        var data = await _context.Inventories
            .Include(i => i.Product).Include(i => i.Warehouse)
            .Where(i => i.Quantity <= i.Product.ReorderStockLevel && i.Product.ReorderStockLevel > 0)
            .OrderBy(i => i.Quantity)
            .Take(count)
            .Select(i => new { i.ProductId, ProductName = i.Product.Name, i.Product.SKU, WarehouseName = i.Warehouse.Name, CurrentStock = i.Quantity, i.Product.ReorderStockLevel })
            .ToListAsync();

        return data.Select(d => new LowStockAlertDto(d.ProductId, d.ProductName, d.SKU, d.WarehouseName, d.CurrentStock, d.ReorderStockLevel)).ToList();
    }

    public async Task<List<OrderStatusBreakdownDto>> GetSalesOrderStatusBreakdown()
    {
        var data = await _context.SalesOrders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return data.Select(d => new OrderStatusBreakdownDto(d.Status.ToString(), d.Count)).ToList();
    }

    public async Task<List<OrderStatusBreakdownDto>> GetPurchaseOrderStatusBreakdown()
    {
        var data = await _context.PurchaseOrders
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return data.Select(d => new OrderStatusBreakdownDto(d.Status.ToString(), d.Count)).ToList();
    }

    public async Task<List<RecentOrderDto>> GetRecentSalesOrders(int count)
    {
        var data = await _context.SalesOrders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .Select(o => new { o.Id, o.OrderNumber, CustomerName = o.Customer.CustomerName, o.OrderDate, o.TotalAmount, o.Status })
            .ToListAsync();

        return data.Select(d => new RecentOrderDto(d.Id, d.OrderNumber, d.CustomerName, d.OrderDate, d.TotalAmount, d.Status.ToString())).ToList();
    }

    public async Task<List<RecentOrderDto>> GetRecentPurchaseOrders(int count)
    {
        var data = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(count)
            .Select(p => new { p.Id, p.OrderNumber, SupplierName = p.Supplier.SupplierName, p.PurchaseDate, p.TotalAmount, p.Status })
            .ToListAsync();

        return data.Select(d => new RecentOrderDto(d.Id, d.OrderNumber, d.SupplierName, d.PurchaseDate, d.TotalAmount, d.Status.ToString())).ToList();
    }

    // ── Screenshot-aligned extensions ────────────────────────────────────

    /// <summary>
    /// A daily series across the selected window, plus the matching totals for that window and for
    /// the equally long window immediately before it.
    /// </summary>
    private readonly record struct KpiWindow(decimal Current, decimal Previous, decimal[] Series);

    public Task<DashboardKpiSetDto> GetKpis(int days) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}kpi-set:{days}",
            KpiLifetime,
            ct => BuildKpisAsync(days, ct));

    private async Task<DashboardKpiSetDto> BuildKpisAsync(int days, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        days = Math.Clamp(days, 1, MaxOverviewDays);

        var today = DateTime.UtcNow.Date;
        var from = today.AddDays(-(days - 1));
        // Queries start here so one round trip covers both the window and the one it is compared to.
        var previousFrom = from.AddDays(-days);

        // Issued one after another, not through Task.WhenAll: the scoped ApplicationDbContext cannot
        // run two operations concurrently and throws if they overlap.

        var salesRows = await _context.SalesOrders
            .AsNoTracking()
            .Where(o => o.OrderDate >= previousFrom && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(o => o.TotalAmount) })
            .ToListAsync(ct);
        var salesByDay = salesRows.ToDictionary(x => x.Date.Date, x => x.Amount);

        var purchaseRows = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.PurchaseDate >= previousFrom
                     && p.Status != PurchaseOrderStatus.Cancelled
                     && p.Status != PurchaseOrderStatus.Draft)
            .GroupBy(p => p.PurchaseDate.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(p => p.TotalAmount) })
            .ToListAsync(ct);
        var purchasesByDay = purchaseRows.ToDictionary(x => x.Date.Date, x => x.Amount);

        // New counterpart parties per day. The sparkline plots the running total, so the tile shows
        // the base growing rather than the noisier daily signups.
        var customerRows = await _context.Customers
            .AsNoTracking()
            .Where(c => c.CreatedAt >= previousFrom)
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var newCustomersByDay = customerRows.ToDictionary(x => x.Date.Date, x => (decimal)x.Count);

        var supplierRows = await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.CreatedAt >= previousFrom)
            .GroupBy(s => s.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var newSuppliersByDay = supplierRows.ToDictionary(x => x.Date.Date, x => (decimal)x.Count);

        var pendingRows = await _context.SalesOrders
            .AsNoTracking()
            .Where(o => o.OrderDate >= previousFrom && o.Status == OrderStatus.Pending)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var pendingByDay = pendingRows.ToDictionary(x => x.Date.Date, x => (decimal)x.Count);

        var totalCustomers = await _context.Customers.CountAsync(ct);
        var customersBeforeWindow = await _context.Customers.CountAsync(c => c.CreatedAt < from, ct);

        var totalSuppliers = await _context.Suppliers.CountAsync(ct);
        var suppliersBeforeWindow = await _context.Suppliers.CountAsync(s => s.CreatedAt < from, ct);

        // Inventory value is a point-in-time balance at current cost. StockMovement records
        // quantities but carries no cost column, and Product.CostPrice is today's cost, so a
        // back-dated valuation would be wrong for every period in which prices moved. There is
        // therefore no honest prior window and no honest sparkline here; both are left empty and the
        // tile renders without them. The previous implementation invented a -8% trend.
        var inventoryValue = await _context.Inventories
            .SumAsync(i => (decimal?)(i.Quantity * i.Product.CostPrice), ct) ?? 0m;

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        _logger.LogDebug("GetKpis resolved 10 aggregates over a {Days}-day window in {ElapsedMs:F0} ms", days, elapsedMs);

        var sales = SplitWindow(salesByDay, days, from);
        var purchases = SplitWindow(purchasesByDay, days, from);
        var pending = SplitWindow(pendingByDay, days, from);
        var signups = SplitWindow(newCustomersByDay, days, from);
        var onboarded = SplitWindow(newSuppliersByDay, days, from);

        return new DashboardKpiSetDto(
            new DashboardKpiTileDto("Total Sales", sales.Current, DashboardKpiFormat.Currency,
                Growth(sales.Current, sales.Previous), sales.Series),

            new DashboardKpiTileDto("Total Purchases", purchases.Current, DashboardKpiFormat.Currency,
                Growth(purchases.Current, purchases.Previous), purchases.Series),

            new DashboardKpiTileDto("Inventory Value", inventoryValue, DashboardKpiFormat.Currency,
                null, Array.Empty<decimal>()),

            new DashboardKpiTileDto("Total Customers", totalCustomers, DashboardKpiFormat.Count,
                Growth(totalCustomers, customersBeforeWindow), RunningTotal(customersBeforeWindow, signups.Series)),

            new DashboardKpiTileDto("Total Suppliers", totalSuppliers, DashboardKpiFormat.Count,
                Growth(totalSuppliers, suppliersBeforeWindow), RunningTotal(suppliersBeforeWindow, onboarded.Series)),

            new DashboardKpiTileDto("Pending Orders", pending.Current, DashboardKpiFormat.Count,
                Growth(pending.Current, pending.Previous), pending.Series));
    }

    /// <summary>
    /// Cuts a per-day dictionary into the selected window's series and totals, and into the totals of
    /// the equally long window before it. Days with no activity read as zero, so both the sparkline
    /// and the comparison stay aligned to the calendar.
    /// </summary>
    private static KpiWindow SplitWindow(Dictionary<DateTime, decimal> byDay, int days, DateTime from)
    {
        var series = new decimal[days];
        var current = 0m;
        var previous = 0m;

        for (var i = 0; i < days; i++)
        {
            var value = byDay.GetValueOrDefault(from.AddDays(i), 0m);
            series[i] = value;
            current += value;
            previous += byDay.GetValueOrDefault(from.AddDays(i - days), 0m);
        }

        return new KpiWindow(current, previous, series);
    }

    /// <summary>
    /// Period-over-period change, rounded for display. Null when the prior window was zero: the ratio
    /// is undefined there, and reporting 0% would read as "flat" when the truth is "no baseline".
    /// </summary>
    private static double? Growth(decimal current, decimal previous) =>
        previous == 0 ? null : Math.Round((double)((current - previous) / previous * 100), 1);

    /// <summary>
    /// Turns per-day increments into a running total, for a measure that accumulates over time rather
    /// than being spent.
    /// </summary>
    private static decimal[] RunningTotal(decimal opening, decimal[] daily)
    {
        var result = new decimal[daily.Length];
        var running = opening;
        for (var i = 0; i < daily.Length; i++)
        {
            running += daily[i];
            result[i] = running;
        }
        return result;
    }

    public Task<List<SalesOverviewPointDto>> GetSalesOverview(int days) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}sales-overview:{days}",
            SnapshotLifetime,
            ct => BuildSalesOverviewAsync(days, ct));

    private async Task<List<SalesOverviewPointDto>> BuildSalesOverviewAsync(int days, CancellationToken ct)
    {
        days = Math.Clamp(days, 1, MaxOverviewDays);

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-days);
        var firstDay = now.Date.AddDays(-(days - 1));

        var salesByDayRows = await _context.SalesOrders
            .Where(o => o.OrderDate >= cutoff && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(o => o.TotalAmount) })
            .ToListAsync(ct);

        var profitByDayRows = await _context.SalesOrderItems
            .Where(i => i.SalesOrder.OrderDate >= cutoff && i.SalesOrder.Status != OrderStatus.Cancelled)
            .GroupBy(i => i.SalesOrder.OrderDate.Date)
            .Select(g => new { Date = g.Key, Profit = g.Sum(i => i.LineTotalPrice - i.Product.CostPrice * i.Quantity) })
            .ToListAsync(ct);

        var salesByDay = salesByDayRows.ToDictionary(x => x.Date.Date, x => x.Amount);
        var profitByDay = profitByDayRows.ToDictionary(x => x.Date.Date, x => x.Profit);

        // Months with no orders at all are still emitted, so a gap in activity reads as a zero
        // rather than silently shortening the strip.
        if (days > MonthlyBucketThresholdDays)
        {
            // The final month is cut off at today. A future-dated order in the current month would
            // otherwise be counted in the bucket while the daily view, which stops at today, drops it.
            var endOfToday = now.Date.AddDays(1);
            var monthly = new List<SalesOverviewPointDto>();
            for (var month = new DateTime(firstDay.Year, firstDay.Month, 1); month <= now; month = month.AddMonths(1))
            {
                var next = month.AddMonths(1);
                if (next > endOfToday) next = endOfToday;

                var amount = SumBetween(salesByDay, month, next);
                var profit = SumBetween(profitByDay, month, next);
                monthly.Add(new SalesOverviewPointDto(month.ToString("MMM yyyy"), amount, profit));
            }
            return monthly;
        }

        // Dictionary lookups replace the previous per-day FirstOrDefault scan, which was
        // quadratic in the number of days requested.
        var result = new List<SalesOverviewPointDto>(days);
        for (var offset = days - 1; offset >= 0; offset--)
        {
            var day = firstDay.AddDays(-offset);
            var amount = salesByDay.GetValueOrDefault(day, 0m);

            // Profit is only known for days that actually contain order lines. A day with orders but
            // no matching line rows yields 0, which is the real figure; the previous code substituted
            // amount * 0.26, inventing a flat 26% margin for every day whose profit could not be
            // measured.
            var profit = profitByDay.GetValueOrDefault(day, 0m);
            result.Add(new SalesOverviewPointDto(day.ToString("MMM dd"), amount, profit));
        }

        return result;
    }

    /// <summary>
    /// Sums a per-day series over a half-open month window. Done in memory rather than in the query
    /// so the day grouping stays a plain GroupBy on a column EF can translate.
    /// </summary>
    private static decimal SumBetween(Dictionary<DateTime, decimal> series, DateTime fromInclusive, DateTime toExclusive)
    {
        var total = 0m;
        foreach (var point in series)
        {
            if (point.Key >= fromInclusive && point.Key < toExclusive) total += point.Value;
        }
        return total;
    }

    public Task<List<SalesByCategoryDto>> GetSalesByCategory() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}sales-by-category",
            SnapshotLifetime,
            async ct =>
            {
                var data = await _context.SalesOrderItems
                    .Where(i => i.SalesOrder.Status != OrderStatus.Cancelled)
                    .GroupBy(i => i.Product.Category != null ? i.Product.Category.Name : "Uncategorised")
                    .Select(g => new { Category = g.Key, Amount = g.Sum(i => i.LineTotalPrice) })
                    .ToListAsync(ct);

                var total = data.Sum(x => x.Amount);

                // An empty result is returned as empty. The previous implementation substituted a
                // fixed six-category dataset (Electronics 13768, Fashion 10723, ...) whenever the
                // tenant had no sales, so an empty tenant rendered invented revenue on the dashboard.
                return data
                    .Select(d => new SalesByCategoryDto(d.Category, d.Amount, total == 0 ? 0 : Math.Round((double)(d.Amount / total * 100), 1)))
                    .OrderByDescending(x => x.Amount)
                    .ToList();
            });

    public Task<List<PaymentMethodBreakdownDto>> GetPaymentMethodBreakdown() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}payment-methods",
            SnapshotLifetime,
            async ct =>
            {
                // Measured from recorded payments. The previous implementation ignored the Payment
                // table entirely and returned five hard-coded percentages (Cash 42.3%, Card 28.7%,
                // ...) against an invented 48520 total, so the card never reflected real activity.
                var data = await _context.Payments
                    .AsNoTracking()
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Amount) })
                    .ToListAsync(ct);

                var total = data.Sum(x => x.Amount);

                return data
                    .Select(d => new PaymentMethodBreakdownDto(
                        d.Method.ToString(),
                        Math.Round(d.Amount, 2),
                        total == 0 ? 0 : Math.Round((double)(d.Amount / total * 100), 1)))
                    .OrderByDescending(x => x.Amount)
                    .ToList();
            });

    public async Task<InventoryStatusDto> GetInventoryStatus()
    {
        // Cached because the tile is re-rendered on every dashboard load.
        return await _cache.GetOrCreateAsync(
            $"{CachePrefix}inventory-status",
            SnapshotLifetime,
            async ct =>
            {
                // One aggregate per product, so the counts are products and not inventory rows.
                // Counting rows would inflate the percentages whenever a product is stocked in more
                // than one warehouse. Selecting from Products also includes products that have no
                // inventory rows at all: their Sum is 0, so they count as out of stock.
                var perProduct = await _context.Products
                    .AsNoTracking()
                    .Select(p => new
                    {
                        p.ReorderStockLevel,
                        TotalQuantity = p.Inventories.Sum(i => i.Quantity)
                    })
                    .ToListAsync(ct);

                var totalProducts = perProduct.Count;
                var inStock = perProduct.Count(p => p.TotalQuantity > 0);
                var lowStock = perProduct.Count(p =>
                    p.ReorderStockLevel > 0 && p.TotalQuantity > 0 && p.TotalQuantity <= p.ReorderStockLevel);
                var outOfStock = totalProducts - inStock;

                static double Pct(int value, int total) => total == 0 ? 0 : Math.Round((double)value / total * 100, 1);

                // No seeded fallback: an empty catalogue reports zero of everything, which is the
                // truth. It previously reported 2482 / 2124 / 198 / 160 regardless of the tenant.
                return new InventoryStatusDto(
                    totalProducts, inStock, lowStock, outOfStock,
                    Pct(inStock, totalProducts), Pct(lowStock, totalProducts), Pct(outOfStock, totalProducts));
            });
    }

    public Task<List<DailySalesByStoreDto>> GetDailySalesByStore() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}daily-sales-by-store",
            SnapshotLifetime,
            async ct =>
            {
                var data = await _context.SalesOrders
                    .Where(o => o.Status != OrderStatus.Cancelled)
                    .GroupBy(o => o.Branch != null ? o.Branch.Name : "Unassigned")
                    .Select(g => new { Store = g.Key, Sales = g.Sum(o => o.TotalAmount) })
                    .OrderByDescending(x => x.Sales)
                    .Take(4)
                    .ToListAsync(ct);

                return data.Select(d => new DailySalesByStoreDto(d.Store, d.Sales)).ToList();
            });

    public Task<List<BestStoreDto>> GetBestPerformingStores() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}best-stores",
            SnapshotLifetime,
            async ct =>
            {
                // Growth is the real week-over-week change: the same branch grouping is run over the
                // previous seven days and the two totals are compared. The previous implementation
                // returned `new Random(42).NextDouble() * 10 + 5` per row, which re-randomised on
                // every cache rebuild and bore no relation to the data it was displayed beside.
                var now = DateTime.UtcNow;
                var startOfWeek = now.AddDays(-7);
                var prevWeekStart = now.AddDays(-14);

                var current = await BuildStoreTotalsAsync(now, startOfWeek, ct);
                var previous = await BuildStoreTotalsAsync(startOfWeek, prevWeekStart, ct);

                return current
                    .Select(entry =>
                    {
                        previous.TryGetValue(entry.Key, out var prior);
                        var growth = prior > 0m
                            ? Math.Round((double)((entry.Value - prior) / prior * 100), 1)
                            : 0d;
                        return new BestStoreDto(entry.Key, entry.Value, growth);
                    })
                    .ToList();
            });

    private async Task<Dictionary<string, decimal>> BuildStoreTotalsAsync(
        DateTime from, DateTime to, CancellationToken ct)
    {
        var rows = await _context.SalesOrders
            .AsNoTracking()
            .Where(o => o.OrderDate >= from && o.OrderDate < to && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.Branch != null ? o.Branch.Name : "Unassigned")
            .Select(g => new { Store = g.Key, Sales = g.Sum(o => o.TotalAmount) })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.Store, x => x.Sales);
    }

    public Task<List<RecentTransactionDto>> GetRecentTransactions(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}recent-transactions:{count}",
            SnapshotLifetime,
            ct => BuildRecentTransactionsAsync(count, ct));

    private async Task<List<RecentTransactionDto>> BuildRecentTransactionsAsync(int count, CancellationToken ct)
    {
        var salesRows = await _context.SalesOrders
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .Select(o => new { o.OrderNumber, o.OrderDate, o.TotalAmount, StatusText = o.Status.ToString() })
            .ToListAsync(ct);

        var purchaseRows = await _context.PurchaseOrders
            .AsNoTracking()
            .OrderByDescending(p => p.PurchaseDate)
            .Take(count)
            .Select(p => new { p.OrderNumber, OrderDate = p.PurchaseDate, p.TotalAmount, StatusText = "Received" })
            .ToListAsync(ct);

        // Sort on the real timestamp, then format. The previous code ordered by the formatted
        // "3 hours ago" label, which sorts lexicographically and put "Yesterday" above
        // "Just now", so the most-recent list was wrong.
        var ordered = salesRows
            .Select(s => new PendingTransaction(
                IsPurchase: false, s.OrderDate, GetTimeAgo(s.OrderDate), s.OrderNumber, s.TotalAmount, s.StatusText))
            .Concat(purchaseRows
                .Select(p => new PendingTransaction(
                    IsPurchase: true, p.OrderDate, GetTimeAgo(p.OrderDate), p.OrderNumber, p.TotalAmount, p.StatusText)))
            .OrderByDescending(x => x.Timestamp)
            .Take(count)
            .ToList();

        return ordered
            .Select(x => x.IsPurchase
                ? new RecentTransactionDto("Purchase", $"Purchase #{x.OrderNumber}", x.Label, x.TotalAmount, x.Status, "Received")
                : new RecentTransactionDto(
                    "Sale",
                    $"Sale #{x.OrderNumber}",
                    x.Label,
                    x.TotalAmount,
                    x.Status,
                    x.Status == nameof(OrderStatus.Sold) ? "Completed" : "Pending"))
            .ToList();
    }

    /// <summary>Normalised sales/purchase activity used to merge the two transaction feeds.</summary>
    private sealed record PendingTransaction(
        bool IsPurchase,
        DateTime Timestamp,
        string Label,
        string OrderNumber,
        decimal TotalAmount,
        string Status);

    public Task<List<SystemAlertDto>> GetSystemAlerts(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}system-alerts:{count}",
            SnapshotLifetime,
            async ct =>
            {
                var lowStockRows = await _context.Inventories
                    .Where(i => i.Product.ReorderStockLevel > 0 && i.Quantity <= i.Product.ReorderStockLevel)
                    .OrderBy(i => i.Quantity)
                    .Take(2)
                    .Select(i => new { i.Product.Name, i.Quantity })
                    .ToListAsync(ct);

                var expiringRows = await _context.Inventories
                    .Where(i => i.ExpiryDate != null && i.ExpiryDate <= DateTime.UtcNow.AddDays(5))
                    .OrderBy(i => i.ExpiryDate)
                    .Take(1)
                    .Select(i => new { i.Product.Name })
                    .ToListAsync(ct);

                var alerts = new List<SystemAlertDto>();
                foreach (var item in lowStockRows)
                    alerts.Add(new SystemAlertDto($"Low stock: {item.Name} ({item.Quantity} remaining)", "", "2 mins ago", "error", "warning"));

                foreach (var item in expiringRows)
                    alerts.Add(new SystemAlertDto($"Expiring soon: {item.Name} (5 days)", "", "12 mins ago", "warning", "schedule"));

                // Only conditions that are actually detected are reported. The previous
                // implementation unconditionally appended three invented alerts ("New customer
                // registration", "Supplier payment due: ABC Supplier", "System backup completed")
                // to every tenant's feed regardless of whether any such event had occurred.
                return alerts.Take(count).ToList();
            });

    public Task<List<RecentOrderExtendedDto>> GetRecentOrdersExtended(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}recent-orders:{count}",
            SnapshotLifetime,
            async ct =>
            {
                var data = await _context.SalesOrders
                    .AsNoTracking()
                    .OrderByDescending(o => o.OrderDate)
                    .Take(count)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        Customer = o.Customer != null ? o.Customer.CustomerName : "Unknown",
                        Store = o.Branch != null ? o.Branch.Name : "Unassigned",
                        o.OrderDate,
                        Status = o.Status.ToString(),
                        o.TotalAmount
                    })
                    .ToListAsync(ct);

                return data
                    .Select(d => new RecentOrderExtendedDto(d.Id, d.OrderNumber, d.Customer, d.Store, d.OrderDate, d.Status, d.TotalAmount))
                    .ToList();
            });

    private static string GetTimeAgo(DateTime date)
    {
        var span = DateTime.UtcNow - date;
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} mins ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} hour{(span.TotalHours >= 2 ? "s" : "")} ago";
        return $"{(int)span.TotalDays} days ago";
    }

    // ── Purchase Overview ─────────────────────────────────────────────

    public Task<List<PurchaseOverviewPointDto>> GetPurchaseOverview(int days) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}purchase-overview:{days}",
            SnapshotLifetime,
            ct => BuildPurchaseOverviewAsync(days, ct));

    private async Task<List<PurchaseOverviewPointDto>> BuildPurchaseOverviewAsync(int days, CancellationToken ct)
    {
        days = Math.Clamp(days, 1, MaxOverviewDays);

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-days);
        var firstDay = now.Date.AddDays(-(days - 1));

        // Draft and Cancelled purchase orders are excluded: neither represents committed spend, and
        // including drafts would make procurement look busier than it is.
        var rows = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.PurchaseDate >= cutoff
                     && p.Status != PurchaseOrderStatus.Cancelled
                     && p.Status != PurchaseOrderStatus.Draft)
            .GroupBy(p => p.PurchaseDate.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(p => p.TotalAmount), Count = g.Count() })
            .ToListAsync(ct);

        var byDay = new Dictionary<DateTime, (decimal Amount, int Count)>();
        foreach (var row in rows) byDay[row.Date.Date] = (row.Amount, row.Count);

        // Months with no orders at all are still emitted, so a gap in activity reads as a zero
        // rather than silently shortening the strip.
        if (days > MonthlyBucketThresholdDays)
        {
            // The final month is cut off at today. A future-dated order in the current month would
            // otherwise be counted in the bucket while the daily view, which stops at today, drops it.
            var endOfToday = now.Date.AddDays(1);
            var monthly = new List<PurchaseOverviewPointDto>();
            for (var month = new DateTime(firstDay.Year, firstDay.Month, 1); month <= now; month = month.AddMonths(1))
            {
                var next = month.AddMonths(1);
                if (next > endOfToday) next = endOfToday;

                var amount = 0m;
                var count = 0;
                foreach (var day in byDay)
                {
                    if (day.Key < month || day.Key >= next) continue;
                    amount += day.Value.Amount;
                    count += day.Value.Count;
                }
                monthly.Add(new PurchaseOverviewPointDto(month.ToString("MMM yyyy"), amount, count));
            }
            return monthly;
        }

        // Every day in the window is emitted, including days with no orders, so the strip keeps a
        // stable width instead of collapsing when a day happens to be empty.
        var result = new List<PurchaseOverviewPointDto>(days);
        for (var offset = days - 1; offset >= 0; offset--)
        {
            var day = firstDay.AddDays(-offset);
            byDay.TryGetValue(day, out var hit);
            result.Add(new PurchaseOverviewPointDto(day.ToString("MMM dd"), hit.Amount, hit.Count));
        }

        return result;
    }

    // ── Inventory Overview ────────────────────────────────────────────

    public Task<InventoryOverviewDto> GetInventoryOverview() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}inventory-overview",
            SnapshotLifetime,
            ct => BuildInventoryOverviewAsync(ct));

    private async Task<InventoryOverviewDto> BuildInventoryOverviewAsync(CancellationToken ct)
    {
        // Two shapes of aggregate, because they answer different questions. The per-product roll-up
        // counts *products* by health, so a product stocked in three warehouses is counted once
        // rather than three times. The single-row group sums *units and value* across every
        // inventory row. A product with no stock rows at all still appears in the first query with a
        // total of zero, and therefore counts as out of stock.
        var perProduct = await _context.Products
            .AsNoTracking()
            .Select(p => new
            {
                p.ReorderStockLevel,
                OnHand = p.Inventories.Sum(i => i.Quantity)
            })
            .ToListAsync(ct);

        var totals = await _context.Inventories
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                OnHand = g.Sum(i => i.Quantity),
                Reserved = g.Sum(i => i.ReservedQuantity),
                AtCost = g.Sum(i => (decimal?)(i.Quantity * i.Product.CostPrice)),
                AtRetail = g.Sum(i => (decimal?)(i.Quantity * i.Product.SellingPrice))
            })
            .FirstOrDefaultAsync(ct);

        var totalProducts = perProduct.Count;
        var inStock = perProduct.Count(p => p.OnHand > 0);
        var lowStock = perProduct.Count(p =>
            p.ReorderStockLevel > 0 && p.OnHand > 0 && p.OnHand <= p.ReorderStockLevel);
        var outOfStock = totalProducts - inStock;

        var onHand = totals?.OnHand ?? 0;
        var reserved = totals?.Reserved ?? 0;

        return new InventoryOverviewDto(
            totalProducts, inStock, lowStock, outOfStock,
            onHand, reserved, onHand - reserved,
            Math.Round(totals?.AtCost ?? 0m, 2),
            Math.Round(totals?.AtRetail ?? 0m, 2));
    }

    // ── Store Overview ────────────────────────────────────────────────

    public Task<List<StoreOverviewDto>> GetStoreOverview() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}store-overview",
            SnapshotLifetime,
            ct => BuildStoreOverviewAsync(ct));

    private async Task<List<StoreOverviewDto>> BuildStoreOverviewAsync(CancellationToken ct)
    {
        // Grouped over sales order *lines* rather than order headers so that revenue and cost are
        // measured on the same rows. Summing the header TotalAmount would not allow the cost side of
        // the margin to be derived at all, and joining through Branch keeps unattributed orders in
        // the result as "Unassigned" instead of silently dropping them.
        var rows = await _context.SalesOrderItems
            .AsNoTracking()
            .Where(i => i.SalesOrder.Status != OrderStatus.Cancelled)
            .GroupBy(i => i.SalesOrder.Branch != null ? i.SalesOrder.Branch.Name : "Unassigned")
            .Select(g => new
            {
                Store = g.Key,
                Revenue = g.Sum(i => i.LineTotalPrice),
                Cost = g.Sum(i => i.Product.CostPrice * i.Quantity),
                Orders = g.Select(i => i.SalesOrderId).Distinct().Count()
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(ct);

        var total = rows.Sum(x => x.Revenue);

        return rows.Select(r =>
        {
            var revenue = Math.Round(r.Revenue, 2);
            var cost = Math.Round(r.Cost, 2);
            return new StoreOverviewDto(
                r.Store,
                revenue,
                cost,
                Math.Round(revenue - cost, 2),
                r.Orders,
                total == 0 ? 0 : Math.Round((double)(r.Revenue / total * 100), 1));
        }).ToList();
    }

    // ── Stock Levels ──────────────────────────────────────────────────

    public Task<List<StockLevelDto>> GetStockLevels(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}stock-levels:{count}",
            SnapshotLifetime,
            ct => BuildStockLevelsAsync(count, ct));

    private async Task<List<StockLevelDto>> BuildStockLevelsAsync(int count, CancellationToken ct)
    {
        count = Math.Clamp(count, 1, 50);

        // Ordering is done in the database rather than after materialisation so the "most at risk
        // first" ranking holds for the whole table, not just the rows that survived the Take. The
        // CASE expression pushes rows at or below their reorder level to the front, then the
        // product name gives a stable secondary order.
        return await _context.Inventories
            .AsNoTracking()
            .OrderBy(i => i.Quantity <= i.Product.ReorderStockLevel ? 0 : 1)
            .ThenBy(i => i.Product.Name)
            .ThenBy(i => i.Warehouse.Name)
            .Take(count)
            .Select(i => new StockLevelDto(
                i.Id,
                i.ProductId,
                i.Product.Name,
                i.Product.SKU,
                i.WarehouseId,
                i.Warehouse.Name,
                i.Quantity,
                i.ReservedQuantity,
                i.Quantity - i.ReservedQuantity,
                i.Product.ReorderStockLevel,
                i.BinLocation))
            .ToListAsync(ct);
    }

    // ── Recent Activities ─────────────────────────────────────────────

    public Task<List<RecentActivityDto>> GetRecentActivities(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}recent-activities:{count}",
            SnapshotLifetime,
            ct => BuildRecentActivitiesAsync(count, ct));

    private async Task<List<RecentActivityDto>> BuildRecentActivitiesAsync(int count, CancellationToken ct)
    {
        count = Math.Clamp(count, 1, 50);

        // AuditLog is deliberately NOT derived from BaseEntity, so the DbContext's tenant query
        // filter does not apply to it and no implicit TenantId predicate reaches the query. The
        // tenant must therefore be applied explicitly here. Anything reading the audit log without
        // this predicate returns every tenant's change history, including the serialised old and new
        // values of every record they touched.
        var tenantId = _tenantProvider.GetTenantId();

        var rows = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .Select(a => new { a.TableName, a.Action, a.UserId, a.Timestamp, a.RecordId })
            .ToListAsync(ct);

        return rows
            .Select(r => new RecentActivityDto(
                DescribeActivity(r.Action, r.TableName),
                r.RecordId == Guid.Empty ? "" : $"#{r.RecordId.ToString()[..8].ToUpperInvariant()}",
                string.IsNullOrWhiteSpace(r.UserId) ? "system" : r.UserId!,
                GetTimeAgo(r.Timestamp),
                ActivityIcon(r.Action),
                ActivityTone(r.Action)))
            .ToList();
    }

    /// <summary>Renders an audit row as a human sentence, e.g. "Created Sales Order".</summary>
    private static string DescribeActivity(string? action, string? tableName)
    {
        var verb = Normalize(action);
        var noun = SplitPascalCase(tableName);

        if (string.IsNullOrEmpty(noun)) return verb;
        if (string.IsNullOrEmpty(verb)) return noun;

        return $"{verb} {noun}";
    }

    /// <summary>
    /// Inserts a space before each interior capital so "SalesOrder" reads as "Sales Order". An
    /// acronym run is left intact, so "SKU" does not become "S K U".
    /// </summary>
    private static string SplitPascalCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i]) && !char.IsUpper(value[i - 1]))
                builder.Append(' ');

            builder.Append(value[i]);
        }

        return builder.ToString();
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    // Material icon *names* as plain strings. The Infrastructure project does not reference
    // MudBlazor, and the DTO layer stays UI-framework free; the Razor cards resolve these against
    // MudBlazor's Icons.Material.Filled lookup. This matches how SystemAlertDto.Icon already works.
    private static string ActivityIcon(string? action) => action switch
    {
        "Created" => "AddCircle",
        "Updated" => "EditNote",
        "Deleted" => "DeleteOutline",
        _ => "History"
    };

    private static string ActivityTone(string? action) => action switch
    {
        "Created" => "#22c55e",
        "Updated" => "#3b82f6",
        "Deleted" => "#ef4444",
        _ => "#94a3b8"
    };
}
