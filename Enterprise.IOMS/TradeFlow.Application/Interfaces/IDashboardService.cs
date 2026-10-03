using TradeFlow.Application.DTOs;

namespace TradeFlow.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardKpiDto> GetDashboardKPIs();
    Task<List<MonthlySalesDto>> GetMonthlySalesData(int months);
    Task<List<MonthlyPurchaseDto>> GetMonthlyPurchaseData(int months);
    Task<List<TopProductDto>> GetTopProducts(int count);
    Task<List<LowStockAlertDto>> GetLowStockAlerts(int count);
    Task<List<OrderStatusBreakdownDto>> GetSalesOrderStatusBreakdown();
    Task<List<OrderStatusBreakdownDto>> GetPurchaseOrderStatusBreakdown();
    Task<List<RecentOrderDto>> GetRecentSalesOrders(int count);
    Task<List<RecentOrderDto>> GetRecentPurchaseOrders(int count);
    // Screenshot-aligned extensions

    /// <summary>
    /// Headline KPI row covering the <paramref name="days"/>-day window ending today. The range
    /// matches the dashboard filter so the tiles and the charts always describe the same period.
    /// </summary>
    Task<DashboardKpiSetDto> GetKpis(int days);

    Task<List<SalesOverviewPointDto>> GetSalesOverview(int days);
    Task<List<SalesByCategoryDto>> GetSalesByCategory();
    Task<List<PaymentMethodBreakdownDto>> GetPaymentMethodBreakdown();
    Task<InventoryStatusDto> GetInventoryStatus();
    Task<List<DailySalesByStoreDto>> GetDailySalesByStore();
    Task<List<BestStoreDto>> GetBestPerformingStores();
    Task<List<RecentTransactionDto>> GetRecentTransactions(int count);
    Task<List<SystemAlertDto>> GetSystemAlerts(int count);
    Task<List<RecentOrderExtendedDto>> GetRecentOrdersExtended(int count);
    Task<List<PurchaseOverviewPointDto>> GetPurchaseOverview(int days);
    Task<InventoryOverviewDto> GetInventoryOverview();
    Task<List<StoreOverviewDto>> GetStoreOverview();
    Task<List<StockLevelDto>> GetStockLevels(int count);
    Task<List<RecentActivityDto>> GetRecentActivities(int count);
}
