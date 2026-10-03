using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.DTOs;




// Tax
public record TaxRateDto(Guid Id, string Name, decimal Rate, TaxType TaxType,
    Guid? TaxJurisdictionId, string? JurisdictionName, DateTime EffectiveFrom,
    DateTime? EffectiveTo, bool IsActive, bool IsCompound);

public record CreateTaxRateDto(string Name, string? Code, decimal Rate, TaxType TaxType,
    bool IsCompound, bool IsActive = true, Guid? TaxJurisdictionId = null);

public record TaxJurisdictionDto(Guid Id, string Name, string Code, string? Country,
    string? State, List<TaxRateDto> TaxRates);

// Pricing
public record PriceListDto(Guid Id, string Name, Guid? CurrencyId, string? CurrencyCode,
    bool IsDefault, DateTime? EffectiveFrom, DateTime? EffectiveTo, bool IsActive,
    List<PriceListItemDto> Items);

public record PriceListItemDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU,
    decimal UnitPrice, int? MinQuantity);

public record CreatePriceListDto(string Name, string? Description = null,
    DateTime? EffectiveFrom = null, DateTime? EffectiveTo = null,
    bool IsActive = true, Guid? CurrencyId = null, bool IsDefault = false,
    List<CreatePriceListItemDto>? Items = null);

public record CreatePriceListItemDto(Guid ProductId, decimal UnitPrice, int? MinQuantity);

public record DiscountDto(Guid Id, string Name, DiscountType Type, decimal Value,
    int? MinQuantity, int? MaxQuantity, DateTime? StartDate, DateTime? EndDate,
    Guid? ProductId, Guid? CategoryId, Guid? CustomerId, bool IsActive);

public record CreateDiscountDto(string Name, DiscountType DiscountType, decimal Value,
    int? MinQuantity = null, DateTime? StartDate = null, DateTime? EndDate = null,
    bool IsActive = true, Guid? ProductId = null, Guid? CustomerId = null);

public record ResolvedPriceDto(decimal UnitPrice, string PriceListName, decimal DiscountAmount,
    decimal FinalPrice);

// Currency
public record CurrencyDto(Guid Id, string Code, string Name, string Symbol,
    int DecimalPlaces, bool IsBaseCurrency, bool IsActive);

public record ExchangeRateDto(Guid Id, Guid FromCurrencyId, string FromCurrencyCode,
    Guid ToCurrencyId, string ToCurrencyCode, decimal Rate, DateTime EffectiveDate);

// Shipping
public record DeliveryNoteDto(Guid Id, string DeliveryNoteNumber, Guid SalesOrderId,
    string SalesOrderNumber, DateTime Date, string? ShippedBy, string? TrackingNumber);

public record CreateDeliveryNoteDto(Guid SalesOrderId, Guid? WarehouseId,
    DateTime? ShippedDate, string? Notes);

public record ShipmentDto(Guid Id, Guid? DeliveryNoteId, string? Carrier, string? TrackingNumber,
    DateTime? ShippedDate, DateTime? DeliveredDate, ShipmentStatus Status);

public record CreateShipmentDto(Guid DeliveryNoteId, string Carrier, string? TrackingNumber,
    DateTime? ShippedDate);

// Stocktake
public record StocktakeDto(Guid Id, Guid WarehouseId, string WarehouseName, DateTime StartDate,
    DateTime? EndDate, StocktakeStatus Status, int TotalItems, int CountedItems);

public record StocktakeItemDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU,
    int SystemQuantity, int? CountedQuantity, int Variance);

public record StocktakeVarianceDto(Guid StocktakeId, string WarehouseName,
    List<StocktakeItemDto> Items, int TotalVariance, decimal ValueVariance);

// Purchase Returns
//public record PurchaseReturnDto(Guid Id, string ReturnNumber, Guid PurchaseOrderId,
//    string PurchaseOrderNumber, PurchaseReturnStatus Status, decimal TotalAmount,
//    string? Reason, List<PurchaseReturnItemDto> Items);

//public record PurchaseReturnItemDto(Guid Id, Guid ProductId, string ProductName,
//    int Quantity, decimal UnitCost, decimal LineTotal);

//public record CreatePurchaseReturnDto(Guid PurchaseOrderId, string? Reason,
//    List<CreatePurchaseReturnItemDto> Items);

//public record CreatePurchaseReturnItemDto(Guid ProductId, int Quantity, decimal UnitCost);

// RFQ
public record RfqRequestDto(Guid Id, string RfqNumber, RfqStatus Status, DateTime? RequiredDate,
    string? Notes, List<RfqItemDto> Items, List<RfqSupplierResponseDto> SupplierResponses);

public record RfqItemDto(Guid Id, Guid ProductId, string ProductName, int Quantity,
    decimal? TargetUnitPrice);

public record RfqSupplierResponseDto(Guid Id, Guid SupplierId, string SupplierName,
    decimal QuotedPrice, int LeadTimeDays, DateTime? ValidUntil, bool IsSelected);

// Credit / Debit Notes
public record CreditNoteDto(Guid Id, string CreditNoteNumber, Guid InvoiceId, string InvoiceNumber,
    DateTime Date, decimal Amount, string? Reason, CreditNoteStatus Status);

public record DebitNoteDto(Guid Id, string DebitNoteNumber, Guid InvoiceId, string InvoiceNumber,
    DateTime Date, decimal Amount, string? Reason, DebitNoteStatus Status);

// Financial Reports
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

// Dashboard
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

// Audit log
public record AuditLogDto(Guid Id, string TableName, Guid RecordId, string Action,
    string? OldValues, string? NewValues, string? UserId, DateTime Timestamp);

// Kits
public record KitDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU,
    bool IsActive, List<KitComponentDto> Components);

public record KitComponentDto(Guid Id, Guid ComponentProductId, string ComponentProductName,
    string ComponentProductSKU, int Quantity);

// UoM
public record UnitOfMeasureDto(Guid Id, string Name, string Abbreviation, bool IsBaseUnit);
public record UoMConversionDto(Guid Id, Guid FromUoMId, string FromUoMName,
    Guid ToUoMId, string ToUoMName, decimal ConversionFactor);

// AR/AP Aging
public record AgingReportDto(string EntityName, Guid EntityId, decimal Current,
    decimal Days1to30, decimal Days31to60, decimal Days61to90, decimal Days90Plus, decimal Total);

// Pagination
public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

// Global Search
public record GlobalSearchResultDto(string EntityType, Guid Id, string Title, string SubTitle, string Url);

/// <summary>
/// The minimum a product picker needs. Ten-plus components were fetching the whole Product entity
/// graph and materialising it in order to bind a dropdown that only ever reads the name.
/// </summary>
public record ProductOptionDto(Guid Id, string Name, string Sku, decimal SellingPrice, decimal CostPrice);

// Tax Calculation
public record TaxCalculationResult(decimal Amount, decimal TaxAmount, decimal TaxRate, string? TaxName, Guid? TaxRateId);

// Tax Jurisdiction
public record CreateTaxJurisdictionDto(string Name, string Code, string Country, string? State);

// Currency Creation
public record CreateCurrencyDto(string Code, string Name, string Symbol, bool IsBaseCurrency = false);

// Payment Recording
public record RecordPaymentDto(PaymentType PaymentType, Guid? CustomerId, Guid? SupplierId,
    Guid? InvoiceId, decimal Amount, PaymentMethod PaymentMethod,
    string? Reference, DateTime PaymentDate);

// Credit/Debit Note Creation
public record CreateCreditNoteDto(Guid InvoiceId, decimal Amount, string? Reason);
public record CreateDebitNoteDto(Guid InvoiceId, decimal Amount, string? Reason);

// RFQ Creation
public record CreateRfqRequestDto(DateTime? RequiredDate, string? Notes, List<CreateRfqItemDto> Items);
public record CreateRfqItemDto(Guid ProductId, int Quantity, decimal? TargetUnitPrice);
public record CreateRfqSupplierResponseDto(Guid RfqRequestId, Guid SupplierId, decimal QuotedPrice,
    int LeadTimeDays, DateTime? ValidUntil, string? Notes);

// Stocktake Creation
public record CreateStocktakeDto(Guid WarehouseId, string? Notes);
public record RecordStocktakeCountDto(Guid StocktakeItemId, int CountedQuantity, string? Notes);

// Kit Creation
public record CreateKitDto(Guid ProductId, List<CreateKitComponentDto> Components);
public record CreateKitComponentDto(Guid ComponentProductId, int Quantity);
public record KitAssemblyDto(Guid KitId, Guid WarehouseId, int Quantity);

// Bank Reconciliation
public record BankStatementDto(Guid Id, string BankAccountName, DateTime StatementDate,
    string? FileName, DateTime ImportedAt, bool IsReconciled, List<BankStatementLineDto> Lines);
public record BankStatementLineDto(Guid Id, DateTime TransactionDate, string? Description,
    decimal Amount, string? Reference, Guid? MatchedPaymentId, bool IsMatched);
public record CreateBankStatementDto(string BankAccountName, DateTime StatementDate, string? FileName);
public record CreateBankStatementLineDto(DateTime TransactionDate, string? Description,
    decimal Amount, string? Reference);

// Landed Cost
public record LandedCostComponentDto(Guid Id, string Name, string? Type, decimal DefaultRate);
public record LandedCostAllocationDto(Guid Id, Guid? PurchaseOrderId, string? PurchaseOrderNumber,
    Guid ComponentId, string ComponentName, decimal Amount, LandedCostAllocMethod AllocationMethod);
public record CreateLandedCostComponentDto(string Name, string? Type, decimal DefaultRate);
public record CreateLandedCostAllocationDto(Guid? PurchaseOrderId, Guid ComponentId,
    decimal Amount, LandedCostAllocMethod AllocationMethod);

// Document Template
public record DocumentTemplateDto(Guid Id, string Name, string Type, string HtmlContent, bool IsDefault);
public record CreateDocumentTemplateDto(string Name, string Type, string HtmlContent, bool IsDefault);

// Approval Workflow
public record ApprovalWorkflowRuleDto(Guid Id, string DocumentType, string? Condition,
    decimal? AmountThreshold, string? ApproverRoleOrUserId, int Level, bool IsActive);
public record CreateApprovalWorkflowRuleDto(string DocumentType, string? Condition,
    decimal? AmountThreshold, string? ApproverRoleOrUserId, int Level);
public record ApprovalRequestDto(Guid Id, string DocumentType, Guid DocumentId,
    ApprovalDecision Status, int CurrentLevel, List<ApprovalRequestStepDto> Steps);
public record ApprovalRequestStepDto(Guid Id, string ApproverId, int Level,
    ApprovalDecision Decision, DateTime? DecidedAt, string? Notes);

// Webhook
public record WebhookSubscriptionDto(Guid Id, string EventType, string Url, bool IsActive, int DeliveryCount);
public record CreateWebhookSubscriptionDto(string EventType, string Url, string Secret);
public record WebhookDeliveryLogDto(Guid Id, string EventType, int? StatusCode, int Attempt,
    DateTime SentAt, bool IsSuccess);

// Custom Fields
public record CustomFieldDefinitionDto(Guid Id, string EntityType, string FieldName,
    CustomFieldType FieldType, bool IsRequired, string? Options, int SortOrder);
public record CreateCustomFieldDefinitionDto(string EntityType, string FieldName,
    CustomFieldType FieldType, bool IsRequired, string? Options);
public record CustomFieldValueDto(Guid Id, Guid DefinitionId, string FieldName, string? Value);

// Notification
public record NotificationLogDto(Guid Id, string UserId, string EventType, NotificationChannel Channel,
    string? Status, DateTime? SentAt);

// Data Privacy
public record DataSubjectRequestDto(Guid Id, DataSubjectRequestType RequestType, string SubjectEmail,
    DataSubjectRequestStatus Status, DateTime? CompletedAt, string? Notes);
public record CreateDataSubjectRequestDto(DataSubjectRequestType RequestType, string SubjectEmail, string? Notes);

// Archival
public record ArchivalPolicyDto(Guid Id, string EntityType, int RetentionDays, bool IsActive, DateTime? LastRunAt);
public record CreateArchivalPolicyDto(string EntityType, int RetentionDays);

// Scheduled Report
public record ScheduledReportDto(Guid Id, string ReportType, string? CronExpression, string? Recipients,
    string? Format, bool IsActive, DateTime? LastRunAt, string? LastRunStatus);
public record CreateScheduledReportDto(string ReportType, string? CronExpression, string? Recipients, string? Format);

// Stock Transfer
public record StockTransferRequestDto(Guid ProductId, Guid SourceWarehouseId, Guid TargetWarehouseId, int Quantity, string? Notes);

// Dead Stock
public record DeadStockItemDto(Guid ProductId, string ProductName, string SKU, string WarehouseName,
    int Quantity, decimal Value, DateTime? LastMovementDate, int DaysSinceLastMovement);

// Notification Template
public record NotificationTemplateDto(Guid Id, string EventType, NotificationChannel Channel,
    string Subject, string BodyTemplate);
public record CreateNotificationTemplateDto(string EventType, NotificationChannel Channel,
    string Subject, string BodyTemplate);

// Exchange Rate Creation
public record CreateExchangeRateDto(Guid FromCurrencyId, Guid ToCurrencyId, decimal Rate, DateTime EffectiveDate);

// User Management
public record UserDto(string Id, string FullName, string? Email, string? Department,
    bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt, List<string> Roles);

public record FilterItem(Guid Id, string Name, bool IsSelect);

// Product Serials
public record ProductSerialDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string SerialNumber,
    string? Barcode,
    string? QRCode,
    DateTime? PurchaseDate,
    DateTime? WarrantyStartDate,
    DateTime? WarrantyEndDate,
    ProductSerialStatus Status,
    Guid? WarehouseId,
    string? WarehouseName,
    string? BinLocation,
    Guid? SupplierId,
    string? SupplierName,
    DateTime CreatedAt);

public record CreateProductSerialDto(
    Guid ProductId,
    string SerialNumber,
    string? Barcode,
    string? QRCode,
    DateTime? PurchaseDate,
    DateTime? WarrantyStartDate,
    DateTime? WarrantyEndDate,
    Guid? WarehouseId,
    string? BinLocation,
    Guid? SupplierId);


