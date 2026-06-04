using IOMS.Domain.Enums;

namespace IOMS.Application.DTOs;

// Products
public record ProductDto(Guid Id, string Name, string SKU, string? Barcode, string? Description,
    decimal CostPrice, decimal SellingPrice, decimal WholeSellingPrice, int ReorderLevel, int MinimumOrderQuantity,
    Guid CategoryId, string? CategoryName, string? ImageUrl, int TotalStock, bool IsKit);

// Extended Product DTO with additional product fields
public record ProductDetailsDto(
    Guid Id,
    string Name,
    string SKU,
    string? Barcode,
    string? Description,
    decimal CostPrice,
    decimal SellingPrice,
    decimal WholeSellingPrice,
    int ReorderLevel,
    int MinimumOrderQuantity,
    Guid CategoryId,
    string? CategoryName,
    string? ImageUrl,
    int TotalStock,
    bool IsKit,
    string? Model,
    string? Brand
) : ProductDto(Id, Name, SKU, Barcode, Description, CostPrice, SellingPrice, WholeSellingPrice, ReorderLevel, MinimumOrderQuantity, CategoryId, CategoryName, ImageUrl, TotalStock, IsKit);

public record CreateProductDto(string Name, string SKU, string? Barcode, string? Description,
    decimal CostPrice, decimal SellingPrice, decimal WholeSellingPrice, int ReorderLevel, int MinimumOrderQuantity,
    Guid CategoryId, Guid? BaseUoMId, string? ImageUrl);

// Extended CreateProductDto
public record CreateProductDetailsDto(
    string Name,
    string SKU,
    string? Barcode,
    string? Description,
    decimal CostPrice,
    decimal SellingPrice,
    decimal WholeSellingPrice,    
    int ReorderLevel,
    int MinimumOrderQuantity,
    Guid CategoryId,
    Guid? BaseUoMId,
    string? ImageUrl,
    string? Model,
    string? Brand
);

public record UpdateProductDto(Guid Id, string Name, string SKU, string? Barcode, string? Description,
    decimal CostPrice, decimal SellingPrice, int ReorderLevel, int MinimumOrderQuantity,
    Guid CategoryId, Guid? BaseUoMId, string? ImageUrl);

// Extended UpdateProductDto
public record UpdateProductDetailsDto(
    Guid Id,
    string Name,
    string SKU,
    string? Barcode,
    string? Description,
    decimal CostPrice,
    decimal SellingPrice,
    int ReorderLevel,
    int MinimumOrderQuantity,
    Guid CategoryId,
    Guid? BaseUoMId,
    string? ImageUrl,
    string? Model,
    string? Brand
);

// Categories
public record CategoryDto(Guid Id, string Name, string? Description, Guid? ParentCategoryId,
    string? ParentCategoryName, int? ProductCount , string? Path);

public record CreateCategoryDto(string Name, string? Description, Guid? ParentCategoryId);

// Warehouses
public record WarehouseDto(Guid Id, string Name, string Code, string? Location, string? Address, bool IsActive);
public record CreateWarehouseDto(string Name, string Code, string? Location, string? Address);

// Inventory
public record InventoryDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU,
    Guid WarehouseId, string WarehouseName, int Quantity, int ReservedQuantity,
    int AvailableQuantity, string? BinLocation);

// Extended InventoryDto for tracking
public record InventoryTrackingDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSKU,
    Guid WarehouseId,
    string WarehouseName,
    int Quantity,
    int ReservedQuantity,
    int AvailableQuantity,
    string? BinLocation,
    string? SerialNumber,
    string? BatchNumber,
    DateTime? ExpiryDate,
    string? Condition
);

public record StockAdjustmentDto(Guid ProductId, Guid WarehouseId, int QuantityChange, string Reason);
public record StockTransferDto(Guid ProductId, Guid SourceWarehouseId, Guid TargetWarehouseId, int Quantity);

// Customers
public record CustomerDto(Guid Id, string Name, string? Email, string? Phone, string? Address,
    string? City, string? State, string? Country, decimal CreditLimit, string? PaymentTerms,
    bool IsActive, bool IsTaxExempt);

public record CreateCustomerDto(string Name, string? Email, string? Phone, string? Address,
    string? City, string? State, string? Country, string? PostalCode,
    decimal CreditLimit, string? PaymentTerms, Guid? TaxJurisdictionId,
    Guid? DefaultPriceListId, Guid? DefaultCurrencyId, bool IsTaxExempt);

// Suppliers
public record SupplierDto(Guid Id, string Name, string? Email, string? Phone, string? Address,
    string? City, string? State, string? Country, string? PaymentTerms,
    int LeadTimeDays, decimal Rating, bool IsActive);

public record CreateSupplierDto(string Name, string? Email, string? Phone, string? Address,
    string? City, string? State, string? Country, string? PostalCode,
    string? PaymentTerms, Guid? DefaultCurrencyId, int LeadTimeDays);

// Sales Orders
public record SalesOrderDto(Guid Id, string OrderNumber, Guid CustomerId, string CustomerName,
    DateTime OrderDate, OrderStatus Status, decimal SubTotal, decimal TaxAmount,
    decimal DiscountAmount, decimal FreightAmount, decimal TotalAmount, string? Notes,
    DateTime? ExpectedDeliveryDate, List<SalesOrderItemDto> Items);

public record SalesOrderItemDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU,
    int Quantity, int ShippedQuantity, decimal UnitPrice, decimal DiscountPercent,
    decimal TaxRate, decimal TaxAmount, decimal LineTotal);

public record CreateSalesOrderDto(Guid CustomerId, Guid? WarehouseId, string? Notes,
    string? ShippingAddress, DateTime? ExpectedDeliveryDate, Guid? CurrencyId,
    List<CreateSalesOrderItemDto> Items);

public record CreateSalesOrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice,
    decimal DiscountPercent, Guid? UoMId);

// Purchase Orders
public record PurchaseOrderDto(Guid Id, string OrderNumber, Guid SupplierId, string SupplierName,
    DateTime OrderDate, PurchaseOrderStatus Status, decimal SubTotal, decimal TaxAmount,
    decimal TotalAmount, string? Notes, DateTime? ExpectedDeliveryDate,
    List<PurchaseOrderItemDto> Items);

public record PurchaseOrderItemDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU,
    int Quantity, int ReceivedQuantity, decimal UnitPrice, decimal TaxRate,
    decimal TaxAmount, decimal LineTotal);

public record CreatePurchaseOrderDto(Guid SupplierId, Guid? WarehouseId, string? Notes,
    DateTime? ExpectedDeliveryDate, Guid? CurrencyId,
    List<CreatePurchaseOrderItemDto> Items);

public record CreatePurchaseOrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice, Guid? UoMId);

public record GoodsReceivedLineDto(Guid PurchaseOrderItemId, Guid ProductId, int ReceivedQuantity);

// Accounting
public record AccountDto(Guid Id, string Code, string Name, AccountType AccountType,
    Guid? ParentAccountId, string? ParentAccountName, bool IsActive, bool IsSystemAccount,
    string? Description, decimal Balance);

public record CreateAccountDto(string Code, string Name, AccountType AccountType,
    Guid? ParentAccountId, string? Description);

public record JournalEntryDto(Guid Id, string Reference, DateTime EntryDate, string? Description,
    bool IsAutoPosted, bool IsReversed, List<JournalEntryLineDto> Lines);

public record JournalEntryLineDto(Guid Id, Guid AccountId, string AccountCode, string AccountName,
    decimal Debit, decimal Credit, string? Description);

public record CreateJournalEntryDto(DateTime EntryDate, string? Description,
    List<CreateJournalEntryLineDto> Lines);

public record CreateJournalEntryLineDto(Guid AccountId, decimal Debit, decimal Credit, string? Description);

// Invoices
public record InvoiceDto(Guid Id, string InvoiceNumber, InvoiceType InvoiceType, InvoiceStatus Status,
    Guid? CustomerId, string? CustomerName, Guid? SupplierId, string? SupplierName,
    DateTime InvoiceDate, DateTime DueDate, decimal SubTotal, decimal TaxAmount,
    decimal TotalAmount, decimal PaidAmount, decimal BalanceDue);

public record CreateInvoiceDto(InvoiceType InvoiceType, Guid? SalesOrderId, Guid? PurchaseOrderId,
    Guid? CustomerId, Guid? SupplierId, DateTime DueDate, string? Notes);

// Payments
public record PaymentDto(Guid Id, Guid InvoiceId, string? InvoiceNumber, decimal Amount,
    DateTime PaymentDate, PaymentMethod Method, string? Reference, string? Notes);

public record CreatePaymentDto(Guid InvoiceId, decimal Amount, DateTime PaymentDate,
    PaymentMethod Method, string? Reference, string? Notes);

// Sales Quotes
public record SalesQuoteDto(Guid Id, string QuoteNumber, Guid CustomerId, string CustomerName,
    QuoteStatus Status, DateTime QuoteDate, DateTime ValidUntil, int Version,
    decimal SubTotal, decimal TaxAmount, decimal DiscountAmount, decimal TotalAmount,
    string? Notes, List<SalesQuoteItemDto> Items);

public record SalesQuoteItemDto(Guid Id, Guid ProductId, string ProductName, string ProductSKU,
    int Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxRate,
    decimal TaxAmount, decimal LineTotal);

public record CreateSalesQuoteDto(Guid CustomerId, DateTime QuoteDate, DateTime ValidUntil,
    string? Notes, List<CreateSalesQuoteItemDto> Items, Guid? CurrencyId = null);

public record CreateSalesQuoteItemDto(Guid ProductId, int Quantity, decimal UnitPrice,
    decimal DiscountPercent);

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
public record PurchaseReturnDto(Guid Id, string ReturnNumber, Guid PurchaseOrderId,
    string PurchaseOrderNumber, PurchaseReturnStatus Status, decimal TotalAmount,
    string? Reason, List<PurchaseReturnItemDto> Items);

public record PurchaseReturnItemDto(Guid Id, Guid ProductId, string ProductName,
    int Quantity, decimal UnitCost, decimal LineTotal);

public record CreatePurchaseReturnDto(Guid PurchaseOrderId, string? Reason,
    List<CreatePurchaseReturnItemDto> Items);

public record CreatePurchaseReturnItemDto(Guid ProductId, int Quantity, decimal UnitCost);

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
    string WarehouseName, int CurrentStock, int ReorderLevel);
public record OrderStatusBreakdownDto(string Status, int Count);
public record RecentOrderDto(Guid Id, string OrderNumber, string CustomerOrSupplier,
    DateTime Date, decimal TotalAmount, string Status);

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
