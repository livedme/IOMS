namespace TradeFlow.Application.DTOs;

public record DashboardKpiDto(int TotalProducts, int ActiveCustomers, int ActiveSuppliers,
    decimal SalesThisMonth, decimal PurchasesThisMonth,
    int PendingSalesOrders, int PendingPurchaseOrders,
    int LowStockItems, decimal ArBalance, decimal ApBalance,
    int OverdueInvoices, decimal InventoryValue);

public record MonthlySalesDto(string Period, decimal Amount, int OrderCount);
public record MonthlyPurchaseDto(string Period, decimal Amount, int OrderCount);
public record TopProductDto(string ProductName, decimal Revenue, int UnitsSold);
public record LowStockAlertDto(Guid ProductId, string ProductName, string SKU,
    string WarehouseName, int CurrentStock, int ReorderStockLevel);
public record OrderStatusBreakdownDto(string Status, int Count);
public record RecentOrderDto(Guid Id, string OrderNumber, string CustomerOrSupplier,
    DateTime Date, decimal TotalAmount, string Status);

// ── Screenshot-aligned dashboard DTOs ──────────────────────────────
/// <summary>How a KPI tile renders its headline figure.</summary>
public enum DashboardKpiFormat
{
    Currency,
    Count
}

/// <summary>
/// One headline KPI. <see cref="Value"/> covers the selected range and <see cref="Growth"/> is the
/// change against the immediately preceding window of equal length. Growth is null whenever that
/// comparison does not exist — either the measure has no stored history, or the prior window was
/// empty and the ratio is undefined — and the tile then omits the arrow rather than printing a
/// percentage nobody computed. <see cref="Trend"/> is the per-day series behind the sparkline, and is
/// empty for the same reason; the tile renders without a line instead of with an invented one.
/// </summary>
public record DashboardKpiTileDto(
    string Label,
    decimal Value,
    DashboardKpiFormat Format,
    double? Growth,
    IReadOnlyList<decimal> Trend);

/// <summary>
/// The headline KPI row. Named fields rather than a list so the row cannot silently re-order or drop
/// a tile, and so the page's placeholder is obvious when a read fails.
/// </summary>
public record DashboardKpiSetDto(
    DashboardKpiTileDto TotalSales,
    DashboardKpiTileDto TotalPurchases,
    DashboardKpiTileDto InventoryValue,
    DashboardKpiTileDto TotalCustomers,
    DashboardKpiTileDto TotalSuppliers,
    DashboardKpiTileDto PendingOrders)
{
    /// <summary>
    /// Placeholder rendered during the first load and after a failed read. Every tile is empty and
    /// trendless, which is visibly different from a genuine all-zero result.
    /// </summary>
    public static DashboardKpiSetDto Empty { get; } = new(
        new DashboardKpiTileDto("Total Sales", 0m, DashboardKpiFormat.Currency, null, Array.Empty<decimal>()),
        new DashboardKpiTileDto("Total Purchases", 0m, DashboardKpiFormat.Currency, null, Array.Empty<decimal>()),
        new DashboardKpiTileDto("Inventory Value", 0m, DashboardKpiFormat.Currency, null, Array.Empty<decimal>()),
        new DashboardKpiTileDto("Total Customers", 0m, DashboardKpiFormat.Count, null, Array.Empty<decimal>()),
        new DashboardKpiTileDto("Total Suppliers", 0m, DashboardKpiFormat.Count, null, Array.Empty<decimal>()),
        new DashboardKpiTileDto("Pending Orders", 0m, DashboardKpiFormat.Count, null, Array.Empty<decimal>()));
}

public record SalesByCategoryDto(string Category, decimal Amount, double Percentage);
public record PaymentMethodBreakdownDto(string Method, decimal Amount, double Percentage);
public record InventoryStatusDto(int TotalProducts, int InStock, int LowStock, int OutOfStock,
    double InStockPct, double LowStockPct, double OutOfStockPct);
public record DailySalesByStoreDto(string StoreName, decimal Sales);
public record BestStoreDto(string StoreName, decimal Sales, double GrowthPct);
public record RecentTransactionDto(string Type, string Number, string TimeAgo, decimal Amount, string Status, string StatusColor);
public record SystemAlertDto(string Title, string Detail, string TimeAgo, string AlertType, string Icon);
public record RecentOrderExtendedDto(Guid Id, string OrderNumber, string Customer, string Store, DateTime Date, string Status, decimal Amount);
public record SalesOverviewPointDto(string Label, decimal TotalSales, decimal Profit);

/// <summary>One day (or month) of purchasing activity for the Purchase Overview strip.</summary>
public record PurchaseOverviewPointDto(string Label, decimal TotalPurchases, int OrderCount);

/// <summary>
/// Aggregate inventory position across every product and warehouse: how much stock is on hand,
/// how much of it is already committed to orders, and what it is worth at cost and at retail.
/// </summary>
public record InventoryOverviewDto(
    int TotalProducts, int InStock, int LowStock, int OutOfStock,
    int TotalQuantity, int ReservedQuantity, int AvailableQuantity,
    decimal InventoryValueAtCost, decimal InventoryValueAtRetail);

/// <summary>
/// Per-store trading performance. Revenue and cost are both derived from sales order lines so the
/// margin is measured, and <paramref name="SharePct"/> is the store's slice of tenant-wide revenue.
/// </summary>
public record StoreOverviewDto(string StoreName, decimal Revenue, decimal Cost, decimal Profit, int Orders, double SharePct);

/// <summary>
/// A single entry in the dashboard activity feed, sourced from the audit log rather than invented,
/// so it reflects who actually changed what and when.
/// </summary>
public record RecentActivityDto(string Title, string Detail, string Actor, string TimeAgo, string Icon, string Tone);
