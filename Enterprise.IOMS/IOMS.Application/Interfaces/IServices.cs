using IOMS.Application.DTOs;
using IOMS.Domain.Enums;

namespace IOMS.Application.Interfaces;


public interface IInventoryService
{
    Task<int> GetStockLevel(Guid productId, Guid warehouseId);
    Task AdjustStock(Guid productId, Guid warehouseId, int quantityChange, string reason);
    Task TransferStock(Guid productId, Guid sourceWarehouseId, Guid targetWarehouseId, int quantity);
    Task<List<LowStockAlertDto>> GetLowStockAlerts();
    Task<PagedResult<InventoryDto>> GetInventoryByWarehouse(Guid warehouseId, int page, int pageSize);

    // Extended inventory tracking
    Task<PagedResult<InventoryTrackingDto>> GetInventoryTrackingByWarehouse(Guid warehouseId, int page, int pageSize);
}

public interface IOrderService
{
    Task<Guid> CreateSalesOrder(CreateSalesOrderDto dto);
    Task ApproveOrder(Guid orderId);
    Task CancelOrder(Guid orderId, string reason);
    Task UpdateOrderStatus(Guid orderId, OrderStatus newStatus);
    Task<SalesOrderDto?> GetSalesOrderById(Guid id);
    Task<PagedResult<SalesOrderDto>> GetSalesOrders(string? search, List<OrderStatus?>? status, int page, int pageSize);
}

public interface IPurchaseService
{
    Task<Guid> CreatePurchaseOrder(CreatePurchaseOrderDto dto);
    Task ApprovePurchaseOrder(Guid poId);
    Task ReceiveGoods(Guid poId, List<GoodsReceivedLineDto> lines);
    Task<PurchaseOrderDto?> GetPurchaseOrderById(Guid id);
    Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrders(string? search, PurchaseOrderStatus? status, int page, int pageSize);
}

public interface IAccountingService
{
    Task<Guid> CreateJournalEntry(CreateJournalEntryDto dto);
    Task AutoPostSalesEntry(Guid salesOrderId, decimal amount);
    Task AutoPostPurchaseEntry(Guid purchaseOrderId, decimal amount);
    Task ReverseJournalEntry(Guid journalEntryId, string reason);
    Task<TrialBalanceDto> GetTrialBalance(DateTime asOfDate);
    Task<ProfitAndLossDto> GetProfitAndLoss(DateTime from, DateTime to);
    Task<BalanceSheetDto> GetBalanceSheet(DateTime asOfDate);
    Task<PagedResult<JournalEntryDto>> GetJournalEntries(string? search, int page, int pageSize);
    Task<PagedResult<AccountDto>> GetAccounts(string? search, AccountType? type, int page, int pageSize);
    Task<List<AccountDto>> GetChartOfAccounts();
    Task<Guid> CreateAccount(CreateAccountDto dto);
    Task RecordPayment(CreatePaymentDto dto);
    Task RecordPayment(RecordPaymentDto dto);
    Task<List<AgingReportDto>> GetArAging();
    Task<List<AgingReportDto>> GetApAging();
}

public interface ITaxService
{
    Task<List<TaxRateDto>> GetTaxRates();
    Task<Guid> CreateTaxRate(CreateTaxRateDto dto);
    Task UpdateTaxRate(Guid id, CreateTaxRateDto dto);
    Task<List<TaxJurisdictionDto>> GetJurisdictions();
    Task<Guid> CreateJurisdiction(CreateTaxJurisdictionDto dto);
    Task<decimal> CalculateTax(Guid jurisdictionId, decimal amount);
}

public interface IPricingService
{
    Task<List<PriceListDto>> GetPriceLists();
    Task<Guid> CreatePriceList(CreatePriceListDto dto);
    Task AddPriceListItem(Guid priceListId, CreatePriceListItemDto dto);
    Task<decimal> GetEffectivePrice(Guid productId, Guid? customerId, decimal quantity);
    Task<List<DiscountDto>> GetDiscounts();
    Task<Guid> CreateDiscount(CreateDiscountDto dto);
    Task<List<CurrencyDto>> GetCurrencies();
    Task<Guid> CreateCurrency(CreateCurrencyDto dto);
    Task<decimal> ConvertCurrency(decimal amount, Guid fromCurrencyId, Guid toCurrencyId);
}

public interface IQuotationService
{
    Task<PagedResult<SalesQuoteDto>> GetSalesQuotes(string? search, QuoteStatus? status, int page, int pageSize);
    Task<SalesQuoteDto> GetSalesQuoteById(Guid id);
    Task<Guid> CreateSalesQuote(CreateSalesQuoteDto dto);
    Task<Guid> ConvertQuoteToOrder(Guid quoteId);
    Task AcceptQuote(Guid quoteId);
    Task RejectQuote(Guid quoteId, string reason);
}

public interface IShippingService
{
    Task<Guid> CreateDeliveryNote(CreateDeliveryNoteDto dto);
    Task<Guid> CreateShipment(CreateShipmentDto dto);
    Task UpdateShipmentStatus(Guid id, ShipmentStatus status);
    Task<PagedResult<ShipmentDto>> GetShipments(string? search, int page, int pageSize);
}

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
}

public interface ISearchService
{
    Task<List<GlobalSearchResultDto>> Search(string query, int maxResults = 20);
}

public interface IStocktakeService
{
    Task<PagedResult<StocktakeDto>> GetStocktakes(StocktakeStatus? status, int page, int pageSize);
    Task<StocktakeDto> GetStocktakeById(Guid id);
    Task<StocktakeVarianceDto> GetVarianceReport(Guid stocktakeId);
    Task<Guid> CreateStocktake(CreateStocktakeDto dto);
    Task RecordCount(RecordStocktakeCountDto dto);
    Task ApproveStocktake(Guid stocktakeId);
    Task CancelStocktake(Guid stocktakeId);
}

public interface IPurchaseReturnService
{
    Task<PagedResult<PurchaseReturnDto>> GetPurchaseReturns(string? search, PurchaseReturnStatus? status, int page, int pageSize);
    Task<PurchaseReturnDto> GetPurchaseReturnById(Guid id);
    Task<Guid> CreatePurchaseReturn(CreatePurchaseReturnDto dto);
    Task ApprovePurchaseReturn(Guid id);
    Task CompletePurchaseReturn(Guid id);
}

public interface IRfqService
{
    Task<PagedResult<RfqRequestDto>> GetRfqRequests(string? search, RfqStatus? status, int page, int pageSize);
    Task<RfqRequestDto> GetRfqRequestById(Guid id);
    Task<Guid> CreateRfqRequest(CreateRfqRequestDto dto);
    Task AddSupplierResponse(CreateRfqSupplierResponseDto dto);
    Task AwardRfq(Guid rfqId, Guid supplierResponseId);
    Task<Guid> ConvertRfqToPurchaseOrder(Guid rfqId, Guid supplierResponseId);
}

public interface ICreditDebitNoteService
{
    Task<PagedResult<CreditNoteDto>> GetCreditNotes(string? search, int page, int pageSize);
    Task<Guid> CreateCreditNote(CreateCreditNoteDto dto);
    Task ApproveCreditNote(Guid id);
    Task<PagedResult<DebitNoteDto>> GetDebitNotes(string? search, int page, int pageSize);
    Task<Guid> CreateDebitNote(CreateDebitNoteDto dto);
    Task ApproveDebitNote(Guid id);
}

public interface IBankReconciliationService
{
    Task<List<BankStatementDto>> GetBankStatements();
    Task<BankStatementDto> GetBankStatementById(Guid id);
    Task<Guid> ImportBankStatement(CreateBankStatementDto dto, List<CreateBankStatementLineDto> lines);
    Task MatchLine(Guid bankStatementLineId, Guid paymentId);
    Task UnmatchLine(Guid bankStatementLineId);
    Task ApproveReconciliation(Guid bankStatementId);
}

public interface IKitService
{
    Task<List<KitDto>> GetKits();
    Task<KitDto> GetKitById(Guid id);
    Task<Guid> CreateKit(CreateKitDto dto);
    Task AssembleKit(KitAssemblyDto dto);
    Task DisassembleKit(KitAssemblyDto dto);
}

public interface ILandedCostService
{
    Task<List<LandedCostComponentDto>> GetComponents();
    Task<Guid> CreateComponent(CreateLandedCostComponentDto dto);
    Task<List<LandedCostAllocationDto>> GetAllocations(Guid? purchaseOrderId);
    Task<Guid> AllocateCost(CreateLandedCostAllocationDto dto);
}

public interface IApprovalService
{
    Task<List<ApprovalWorkflowRuleDto>> GetWorkflowRules();
    Task<Guid> CreateWorkflowRule(CreateApprovalWorkflowRuleDto dto);
    Task DeleteWorkflowRule(Guid id);
    Task<PagedResult<ApprovalRequestDto>> GetPendingApprovals(string? userId, int page, int pageSize);
    Task<Guid> SubmitForApproval(string documentType, Guid documentId);
    Task ProcessDecision(Guid approvalRequestStepId, ApprovalDecision decision, string? notes);
}

public interface IDocumentTemplateService
{
    Task<List<DocumentTemplateDto>> GetTemplates();
    Task<Guid> CreateTemplate(CreateDocumentTemplateDto dto);
    Task UpdateTemplate(Guid id, CreateDocumentTemplateDto dto);
    Task DeleteTemplate(Guid id);
}

public interface IWebhookService
{
    Task<List<WebhookSubscriptionDto>> GetSubscriptions();
    Task<Guid> CreateSubscription(CreateWebhookSubscriptionDto dto);
    Task DeleteSubscription(Guid id);
    Task<List<WebhookDeliveryLogDto>> GetDeliveryLogs(Guid subscriptionId);
}

public interface ICustomFieldService
{
    Task<List<CustomFieldDefinitionDto>> GetDefinitions(string? entityType);
    Task<Guid> CreateDefinition(CreateCustomFieldDefinitionDto dto);
    Task DeleteDefinition(Guid id);
    Task<List<CustomFieldValueDto>> GetValues(Guid entityId);
    Task SetValue(Guid definitionId, Guid entityId, string? value);
}

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetAuditLogs(string? tableName, string? action, int page, int pageSize);
}

public interface IScheduledReportService
{
    Task<List<ScheduledReportDto>> GetScheduledReports();
    Task<Guid> CreateScheduledReport(CreateScheduledReportDto dto);
    Task UpdateScheduledReport(Guid id, CreateScheduledReportDto dto);
    Task DeleteScheduledReport(Guid id);
}

public interface IDataPrivacyService
{
    Task<List<DataSubjectRequestDto>> GetRequests();
    Task<Guid> CreateRequest(CreateDataSubjectRequestDto dto);
    Task ProcessRequest(Guid id);
}

public interface IArchivalService
{
    Task<List<ArchivalPolicyDto>> GetPolicies();
    Task<Guid> CreatePolicy(CreateArchivalPolicyDto dto);
    Task UpdatePolicy(Guid id, CreateArchivalPolicyDto dto);
    Task DeletePolicy(Guid id);
}

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProducts(string? search, Guid? categoryId, int page, int pageSize);
    Task<ProductDto?> GetProductById(Guid id);
    Task<ProductDetailsDto?> GetProductDetailsById(Guid id);
    Task<Guid> CreateProduct(CreateProductDto dto);
    Task<Guid> CreateProductDetails(CreateProductDetailsDto dto);
    Task UpdateProduct(UpdateProductDto dto);
    Task UpdateProductDetails(UpdateProductDetailsDto dto);
    Task DeleteProduct(Guid id);
    Task<PagedResult<BrandDto>> GetBrands(string? search, int page, int pageSize);
    Task<List<BrandDto>> GetAllBrands();
    Task<Guid> CreateBrand(CreateBrandDto dto);
    Task DeleteBrand(Guid id);
    Task<PagedResult<CategoryDto>> GetCategories(string? search, int page, int pageSize);
    Task<List<CategoryDto>> GetAllCategories();
    Task<Guid> CreateCategory(CreateCategoryDto dto);
    Task DeleteCategory(Guid id);
    Task<PagedResult<WarehouseDto>> GetWarehouses(string? search, int page, int pageSize);
    Task<List<WarehouseDto>> GetAllWarehouses();
    Task<Guid> CreateWarehouse(CreateWarehouseDto dto);
    Task DeleteWarehouse(Guid id);
}

public interface ICustomerSupplierService
{
    Task<PagedResult<CustomerDto>> GetCustomers(string? search, int page, int pageSize);
    Task<CustomerDto?> GetCustomerById(Guid id);
    Task<Guid> CreateCustomer(CreateCustomerDto dto);
    Task UpdateCustomer(Guid id, CreateCustomerDto dto);
    Task DeleteCustomer(Guid id);
    Task<PagedResult<SupplierDto>> GetSuppliers(string? search, int page, int pageSize);
    Task<SupplierDto?> GetSupplierById(Guid id);
    Task<Guid> CreateSupplier(CreateSupplierDto dto);
    Task UpdateSupplier(Guid id, CreateSupplierDto dto);
    Task DeleteSupplier(Guid id);
}

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetInvoices(string? search, InvoiceStatus? status, InvoiceType? type, int page, int pageSize);
    Task<InvoiceDto?> GetInvoiceById(Guid id);
    Task<Guid> CreateInvoice(CreateInvoiceDto dto);
    Task<Guid> GenerateInvoiceFromSalesOrder(Guid salesOrderId);
    Task<Guid> GenerateInvoiceFromPurchaseOrder(Guid purchaseOrderId);
}

public interface IUoMService
{
    Task<List<UnitOfMeasureDto>> GetUnitsOfMeasure();
    Task<Guid> CreateUnitOfMeasure(string name, string abbreviation, bool isBaseUnit);
    Task DeleteUnitOfMeasure(Guid id);
    Task<List<UoMConversionDto>> GetConversions();
    Task<Guid> CreateConversion(Guid fromUoMId, Guid toUoMId, decimal conversionFactor);
    Task DeleteConversion(Guid id);
}

public interface IExchangeRateService
{
    Task<List<ExchangeRateDto>> GetExchangeRates();
    Task<Guid> CreateExchangeRate(Guid fromCurrencyId, Guid toCurrencyId, decimal rate, DateTime effectiveDate);
    Task DeleteExchangeRate(Guid id);
}

public interface INotificationTemplateService
{
    Task<List<NotificationTemplateDto>> GetTemplates();
    Task<Guid> CreateTemplate(CreateNotificationTemplateDto dto);
    Task UpdateTemplate(Guid id, CreateNotificationTemplateDto dto);
    Task DeleteTemplate(Guid id);
}

public interface IUserManagementService
{
    Task<List<UserDto>> GetUsers(string? search);
    Task<UserDto?> GetUserById(string userId);
    Task ToggleUserActive(string userId);
    Task<List<string>> GetRoles();
    Task<List<string>> GetUserRoles(string userId);
    Task AssignRole(string userId, string role);
    Task RemoveRole(string userId, string role);
    Task CreateRole(string roleName);
    Task DeleteRole(string roleName);
}
