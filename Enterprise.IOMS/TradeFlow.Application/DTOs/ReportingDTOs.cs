using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;

// ─────────────────────────────────────────────────────────────────────────────
// Reporting and detail projections.
//
// These exist because the report pages used to run their own EF queries against the
// page's scoped ApplicationDbContext. The queries are unchanged — they only moved
// behind IReportsService and now return DTOs instead of tracked entities — but the
// shape is fixed here so the pages no longer need a data-access dependency.
// ─────────────────────────────────────────────────────────────────────────────

// ── Sales report ──
public record SalesReportOrderItemDto(string ProductName, int Quantity, decimal LineTotalPrice);

public record SalesReportOrderDto(
    string OrderNumber,
    DateTime OrderDate,
    string CustomerName,
    OrderStatus Status,
    decimal TotalAmount,
    List<SalesReportOrderItemDto> Items);

public record SalesReportInvoiceDto(
    Guid? CustomerId,
    string CustomerName,
    DateTime InvoiceDate,
    decimal TotalAmount);

public record SalesReportPaymentDto(
    Guid? CustomerId,
    DateTime PaymentDate,
    PaymentMethod PaymentMethod,
    decimal Amount);

/// <summary>
/// Source rows for the sales report. The page keeps the grouping, the running totals and
/// the chart series, so the service only has to return the three sets it used to load.
/// </summary>
public record SalesReportDataDto(
    List<SalesReportOrderDto> Orders,
    List<SalesReportInvoiceDto> Invoices,
    List<SalesReportPaymentDto> Payments);

// ── Inventory report ──
public record InventoryStockRowDto(
    string ProductName,
    string SKU,
    string WarehouseName,
    int Quantity,
    int Reserved,
    int Available,
    decimal Value);

public record StockMovementReportRowDto(
    DateTime MovementDate,
    string ProductName,
    string WarehouseName,
    string MovementType,
    int Quantity,
    string? Reference,
    string? Notes);

public record InventoryValuationRowDto(
    string ProductName,
    string SKU,
    int TotalQuantity,
    decimal CostPrice,
    decimal TotalValue);

public record InventoryReportDataDto(
    List<InventoryStockRowDto> Stock,
    List<StockMovementReportRowDto> Movements,
    List<InventoryValuationRowDto> Valuation);

// ── Tax report ──
public record TaxRateReportRowDto(string TaxName, decimal Rate);

public record TaxPeriodRowDto(DateTime Period, decimal OutputTax, decimal InputTax);

public record TaxReportDto(
    decimal OutputTax,
    decimal InputTax,
    decimal NetTax,
    List<TaxRateReportRowDto> TaxRates,
    List<TaxPeriodRowDto> Periods);

// ── Costing and ABC ──
public record InventoryCostingRowDto(
    Guid ProductId,
    string ProductName,
    string SKU,
    int TotalQuantity,
    decimal UnitCost);

public record ProductRevenueRowDto(
    Guid ProductId,
    string ProductName,
    string SKU,
    decimal Revenue,
    int Quantity);

// ── Product detail ──
public record ProductWarehouseStockDto(
    Guid WarehouseId,
    string WarehouseName,
    int Quantity,
    int ReservedQuantity,
    int AvailableQuantity);

public record ProductOrderLineDto(
    Guid OrderId,
    string OrderNumber,
    string PartyName,
    DateTime OrderDate,
    string Status,
    int Quantity,
    decimal UnitPrice);

/// <summary>
/// Everything the product detail screen renders, in one round trip. Replaces eight queries
/// issued straight off the page context, which tracked a product plus its category tree and
/// inventory graph just to render a read-only view.
/// </summary>
public record ProductDetailViewDto(
    Guid Id,
    string Name,
    string SKU,
    string? Barcode,
    string? Description,
    string? ImageUrl,
    decimal CostPrice,
    decimal SellingPrice,
    decimal WholeSellingPrice,
    int ReorderStockLevel,
    int MinOrderQuantity,
    string? Model,
    string? OriginCountry,
    string? OriginManufacturer,
    string? CategoryName,
    string? ParentCategoryName,
    string? BrandName,
    string? PreferredSupplierName,
    int Last30DaysSales,
    int TotalAvailable,
    List<ProductWarehouseStockDto> WarehouseStock,
    List<StockMovementReportRowDto> Movements,
    List<ProductOrderLineDto> SalesOrderLines,
    List<ProductOrderLineDto> PurchaseOrderLines);

// ── Accounting ──
public record StatementInvoiceRowDto(Guid Id, string InvoiceNumber, DateTime InvoiceDate, decimal TotalAmount);

public record StatementPaymentRowDto(Guid Id, string PaymentNumber, DateTime PaymentDate, decimal Amount);

public record CustomerStatementDto(
    List<StatementInvoiceRowDto> Invoices,
    List<StatementPaymentRowDto> Payments);

public record GeneralLedgerLineDto(
    DateTime EntryDate,
    string EntryNumber,
    string AccountCode,
    string AccountName,
    string? Description,
    decimal Debit,
    decimal Credit);

public record PaymentListItemDto(
    string PaymentNumber,
    DateTime PaymentDate,
    PaymentType PaymentType,
    PaymentMethod PaymentMethod,
    decimal Amount,
    string? Reference,
    string PartyName);

// ── Pickers ──
public record PurchaseOrderOptionDto(
    Guid Id,
    string OrderNumber,
    DateTime PurchaseDate,
    PurchaseOrderStatus Status);

public record InvoiceOptionDto(Guid Id, string InvoiceNumber, decimal TotalAmount);

public record TrialBalanceDto(DateTime AsOfDate, List<TrialBalanceLineDto> Lines,
    decimal TotalDebits, decimal TotalCredits);

public record TrialBalanceLineDto(string AccountCode, string AccountName, AccountType AccountType,
    decimal Debit, decimal Credit);

public record ProfitAndLossDto(DateTime From, DateTime To, decimal TotalRevenue,
    decimal TotalExpenses, decimal NetProfit, List<PnlLineDto> RevenueLines,
    List<PnlLineDto> ExpenseLines);

public record PnlLineDto(string AccountCode, string AccountName, decimal Amount);

public record BalanceSheetDto(DateTime AsOfDate, decimal TotalAssets, decimal TotalLiabilities,
    decimal TotalEquity, List<BsLineDto> Assets, List<BsLineDto> Liabilities,
    List<BsLineDto> Equity);

public record BsLineDto(string AccountCode, string AccountName, decimal Amount);

// AR/AP Aging
public record AgingReportDto(string EntityName, Guid EntityId, decimal Current,
    decimal Days1to30, decimal Days31to60, decimal Days61to90, decimal Days90Plus, decimal Total);
