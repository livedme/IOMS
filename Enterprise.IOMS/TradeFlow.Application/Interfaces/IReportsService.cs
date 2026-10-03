using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

/// <summary>
/// Read-only reporting queries behind the reports section. Each method replaces a block of EF
/// that used to sit inside a page's <c>@code</c>; the page still owns the grouping, the running
/// totals and the chart series, so these return rows rather than finished reports.
/// </summary>
public interface IReportsService
{
    /// <summary>Orders with their items, plus sales invoices and customer receipts, for one date window.</summary>
    Task<SalesReportDataDto> GetSalesReportAsync(DateTime from, DateTime to);

    /// <summary>
    /// Stock levels, recent movements and inventory valuation for a warehouse. Only the
    /// non-empty section is populated, matching the report type the page is showing.
    /// </summary>
    Task<InventoryReportDataDto> GetInventoryReportAsync(string reportType, Guid? warehouseId);

    Task<TaxReportDto> GetTaxReportAsync(DateTime from, DateTime to);

    /// <summary>On-hand quantity and unit cost per product. Landed cost is applied by the page.</summary>
    Task<List<InventoryCostingRowDto>> GetInventoryCostingRowsAsync();

    /// <summary>Lifetime revenue per product, ranked, for the ABC classification.</summary>
    Task<List<ProductRevenueRowDto>> GetProductRevenueAsync();

    /// <summary>
    /// On-hand lines whose product/warehouse pair has not moved for at least
    /// <paramref name="daysThreshold"/> days. A line with no movement at all is reported as the
    /// threshold plus one, matching what the page computed inline.
    /// </summary>
    Task<List<DeadStockItemDto>> GetDeadStockAsync(int daysThreshold);
}
