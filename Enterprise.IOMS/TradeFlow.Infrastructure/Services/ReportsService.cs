using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TradeFlow.Infrastructure.Services
{
    /// <summary>
    /// Backing queries for the reports section.
    /// </summary>
    /// <remarks>
    /// Every one of these used to be written inline in a page's <c>@code</c> block, running against
    /// the page's own scoped <c>ApplicationDbContext</c>. That coupled the view to the schema and
    /// left the pages opening a second command on the shared context while a list load was still in
    /// flight. The queries themselves are unchanged.
    /// <para>
    /// Each method takes its own context from <c>IDbContextFactory</c>: reports are heavyweight,
    /// can be re-run from the Generate button, and never need the caller's tracked state, so
    /// sharing the scoped instance buys nothing and risks the "second operation" failure.
    /// </para>
    /// </remarks>
    public class ReportsService : IReportsService
    {
        private readonly ApplicationDbContext _db;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<ReportsService> _logger;

        public ReportsService(
            ApplicationDbContext db,
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ILogger<ReportsService> logger)
        {
            _db = db;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<SalesReportDataDto> GetSalesReportAsync(DateTime from, DateTime to)
        {
            await using var read = await _dbFactory.CreateDbContextAsync();

            var orders = await read.SalesOrders
                .Where(o => o.OrderDate >= from && o.OrderDate < to)
                .AsNoTracking()
                .Select(o => new SalesReportOrderDto(
                    o.OrderNumber,
                    o.OrderDate,
                    o.Customer.CustomerName,
                    o.Status,
                    o.TotalAmount,
                    o.Items
                        .Select(i => new SalesReportOrderItemDto(i.Product.Name, i.Quantity, i.LineTotalPrice))
                        .ToList()))
                .ToListAsync();

            var invoices = await read.Invoices
                .Where(i => i.InvoiceType == InvoiceType.Sales && i.InvoiceDate >= from && i.InvoiceDate < to)
                .AsNoTracking()
                .Select(i => new SalesReportInvoiceDto(
                    i.CustomerId,
                    i.Customer == null ? "Unknown" : i.Customer.CustomerName,
                    i.InvoiceDate,
                    i.TotalAmount))
                .ToListAsync();

            var payments = await read.Payments
                .Where(p => p.PaymentType == PaymentType.Receipt
                            && p.Status == PaymentStatus.Completed
                            && p.PaymentDate >= from && p.PaymentDate < to)
                .AsNoTracking()
                .Select(p => new SalesReportPaymentDto(
                    p.CustomerId,
                    p.PaymentDate,
                    p.PaymentMethod,
                    p.Amount))
                .ToListAsync();

            return new SalesReportDataDto(orders, invoices, payments);
        }

        public async Task<InventoryReportDataDto> GetInventoryReportAsync(string reportType, Guid? warehouseId)
        {
            await using var read = await _dbFactory.CreateDbContextAsync();

            var stock = new List<InventoryStockRowDto>();
            var movements = new List<StockMovementReportRowDto>();
            var valuation = new List<InventoryValuationRowDto>();

            if (reportType == "StockLevel")
            {
                var query = read.Inventories.AsNoTracking();
                if (warehouseId.HasValue)
                    query = query.Where(i => i.WarehouseId == warehouseId.Value);

                stock = await query
                    .Select(i => new InventoryStockRowDto(
                        i.Product.Name,
                        i.Product.SKU,
                        i.Warehouse.Name,
                        i.Quantity,
                        i.ReservedQuantity,
                        i.Quantity - i.ReservedQuantity,
                        i.Quantity * i.Product.CostPrice))
                    .ToListAsync();
            }
            else if (reportType == "Movements")
            {
                var query = read.StockMovements.AsNoTracking();
                if (warehouseId.HasValue)
                    query = query.Where(m => m.WarehouseId == warehouseId.Value);

                movements = await query
                    .OrderByDescending(m => m.MovementDate)
                    .Take(200)
                    .Select(m => new StockMovementReportRowDto(
                        m.MovementDate,
                        m.Product.Name,
                        m.Warehouse.Name,
                        m.Type.ToString(),
                        m.Quantity,
                        m.Reference,
                        m.Notes))
                    .ToListAsync();
            }
            else
            {
                var query = read.Inventories.AsNoTracking();
                if (warehouseId.HasValue)
                    query = query.Where(i => i.WarehouseId == warehouseId.Value);

                valuation = await query
                    .GroupBy(i => new { i.ProductId, i.Product.Name, i.Product.SKU, i.Product.CostPrice })
                    .Select(g => new InventoryValuationRowDto(
                        g.Key.Name,
                        g.Key.SKU,
                        g.Sum(i => i.Quantity),
                        g.Key.CostPrice,
                        g.Sum(i => i.Quantity) * g.Key.CostPrice))
                    .OrderByDescending(v => v.TotalValue)
                    .ToListAsync();
            }

            return new InventoryReportDataDto(stock, movements, valuation);
        }

        public async Task<TaxReportDto> GetTaxReportAsync(DateTime from, DateTime to)
        {
            await using var read = await _dbFactory.CreateDbContextAsync();

            var outputTax = await read.Invoices
                .Where(i => !i.IsDeleted && i.InvoiceDate >= from && i.InvoiceDate <= to)
                .SumAsync(i => i.TaxAmount);

            var inputTax = await read.PurchaseOrders
                .Where(p => !p.IsDeleted && p.PurchaseDate >= from && p.PurchaseDate <= to)
                .SumAsync(p => p.TaxAmount);

            var taxRates = await read.TaxRates
                .Where(t => !t.IsDeleted)
                .AsNoTracking()
                .Select(t => new TaxRateReportRowDto(t.Name, t.Rate))
                .ToListAsync();

            // Grouped in the database rather than materialising every invoice and purchase for the
            // window, which the page did to build the same monthly buckets.
            var monthlyInvoices = await read.Invoices
                .Where(i => !i.IsDeleted && i.InvoiceDate >= from && i.InvoiceDate <= to)
                .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Tax = g.Sum(i => i.TaxAmount) })
                .ToListAsync();

            var monthlyPurchases = await read.PurchaseOrders
                .Where(p => !p.IsDeleted && p.PurchaseDate >= from && p.PurchaseDate <= to)
                .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Tax = g.Sum(p => p.TaxAmount) })
                .ToListAsync();

            var months = Enumerable.Range(0, (int)((to - from).TotalDays / 30) + 1)
                .Select(i => from.AddMonths(i))
                .Select(d => new DateTime(d.Year, d.Month, 1))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var periods = months
                .Select(m => new TaxPeriodRowDto(
                    m,
                    monthlyInvoices.Where(x => x.Year == m.Year && x.Month == m.Month).Sum(x => x.Tax),
                    monthlyPurchases.Where(x => x.Year == m.Year && x.Month == m.Month).Sum(x => x.Tax)))
                .ToList();

            return new TaxReportDto(outputTax, inputTax, outputTax - inputTax, taxRates, periods);
        }

        public async Task<List<InventoryCostingRowDto>> GetInventoryCostingRowsAsync()
        {
            await using var read = await _dbFactory.CreateDbContextAsync();

            return await read.Inventories
                .Where(i => i.Quantity > 0)
                .AsNoTracking()
                .GroupBy(i => new { i.ProductId, i.Product.Name, i.Product.SKU, i.Product.CostPrice })
                .Select(g => new InventoryCostingRowDto(
                    g.Key.ProductId,
                    g.Key.Name,
                    g.Key.SKU,
                    g.Sum(i => i.Quantity),
                    g.Key.CostPrice))
                .ToListAsync();
        }

        public async Task<List<ProductRevenueRowDto>> GetProductRevenueAsync()
        {
            await using var read = await _dbFactory.CreateDbContextAsync();

            return await read.SalesOrderItems
                .AsNoTracking()
                .GroupBy(i => new { i.ProductId, i.Product.Name, i.Product.SKU })
                .Select(g => new ProductRevenueRowDto(
                    g.Key.ProductId,
                    g.Key.Name,
                    g.Key.SKU,
                    g.Sum(i => i.LineTotalPrice),
                    g.Sum(i => i.Quantity)))
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();
        }

        public async Task<List<DeadStockItemDto>> GetDeadStockAsync(int daysThreshold)
        {
            await using var read = await _dbFactory.CreateDbContextAsync();

            var inventories = await read.Inventories
                .Where(i => i.Quantity > 0)
                .AsNoTracking()
                .Select(i => new { i.ProductId, i.WarehouseId, ProductName = i.Product.Name, i.Product.SKU, WarehouseName = i.Warehouse.Name, i.Quantity, Value = i.Quantity * i.Product.CostPrice })
                .ToListAsync();

            var productIds = inventories.Select(i => i.ProductId).Distinct().ToList();

            var lastMovements = await read.StockMovements
                .Where(m => productIds.Contains(m.ProductId))
                .GroupBy(m => new { m.ProductId, m.WarehouseId })
                .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, LastDate = g.Max(m => m.MovementDate) })
                .ToListAsync();

            // A product/warehouse pair with no movement row at all is treated as never having moved,
            // which the page signalled with a sentinel larger than any real age.
            var never = daysThreshold + 909;

            return inventories
                .Select(inv =>
                {
                    var lastDate = lastMovements
                        .FirstOrDefault(m => m.ProductId == inv.ProductId && m.WarehouseId == inv.WarehouseId)?.LastDate;
                    var days = lastDate.HasValue ? (int)(DateTime.UtcNow - lastDate.Value).TotalDays : never;
                    return new DeadStockItemDto(inv.ProductId, inv.ProductName, inv.SKU,
                        inv.WarehouseName, inv.Quantity, inv.Value, lastDate, days);
                })
                .Where(d => d.DaysSinceLastMovement >= daysThreshold)
                .OrderByDescending(d => d.DaysSinceLastMovement)
                .ToList();
        }
    }
}