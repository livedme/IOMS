using TradeFlow.Application.DTOs;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Interfaces;

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

    /// <summary>
    /// Journal lines for the ledger screen, unfiltered when an argument is null. The page was
    /// composing this against the context, including the <c>JournalEntry</c> and <c>Account</c>
    /// includes the projection needs.
    /// </summary>
    Task<List<GeneralLedgerLineDto>> GetGeneralLedgerLines(Guid? accountId, DateTime? from, DateTime? to);

    /// <summary>Most recent payments with their counterparty resolved, for the payments screen.</summary>
    Task<List<PaymentListItemDto>> GetRecentPayments(int count);

    /// <summary>Invoices and payments for one customer over a window, for the statement screen.</summary>
    Task<CustomerStatementDto> GetCustomerStatement(Guid customerId, DateTime from, DateTime to);
}
