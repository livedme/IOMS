using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Constants;
using TradeFlow.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace TradeFlow.Infrastructure.Services;

public class AccountingService : IAccountingService
{
    private readonly IMapper _mapper;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public AccountingService(IMapper mapper, IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _mapper = mapper;
        _dbFactory = dbFactory;
    }

    public async Task<Guid> CreateJournalEntry(CreateJournalEntryDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var totalDebits = dto.Lines.Sum(l => l.Debit);
        var totalCredits = dto.Lines.Sum(l => l.Credit);

        if (Math.Abs(totalDebits - totalCredits) > 0.01m)
            throw new JournalEntryImbalanceException(totalDebits, totalCredits);

        var entry = new JournalEntry
        {
            Reference = NumberGenerator.GenerateJournalReference(),
            EntryDate = dto.EntryDate,
            Description = dto.Description,
            IsAutoPosted = false
        };

        foreach (var line in dto.Lines)
        {
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = line.AccountId,
                Debit = line.Debit,
                Credit = line.Credit,
                Description = line.Description
            });
        }

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry.Id;
    }

    public async Task AutoPostSalesEntry(Guid salesOrderId, decimal amount)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var arAccount = await GetAccountByCode(AccountCodes.AccountsReceivable, _context);
        var revenueAccount = await GetAccountByCode(AccountCodes.SalesRevenue, _context);

        var entry = new JournalEntry
        {
            Reference = NumberGenerator.GenerateJournalReference(),
            EntryDate = DateTime.UtcNow,
            Description = $"Auto-post: Sales Order approved",
            IsAutoPosted = true
        };

        entry.Lines.Add(new JournalEntryLine { AccountId = arAccount.Id, Debit = amount, Credit = 0, Description = "Accounts Receivable" });
        entry.Lines.Add(new JournalEntryLine { AccountId = revenueAccount.Id, Debit = 0, Credit = amount, Description = "Sales Revenue" });

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task AutoPostPurchaseEntry(Guid purchaseOrderId, decimal amount)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var inventoryAccount = await GetAccountByCode(AccountCodes.Inventory, _context);
        var apAccount = await GetAccountByCode(AccountCodes.AccountsPayable, _context);

        var entry = new JournalEntry
        {
            Reference = NumberGenerator.GenerateJournalReference(),
            EntryDate = DateTime.UtcNow,
            Description = $"Auto-post: Goods received",
            IsAutoPosted = true
        };

        entry.Lines.Add(new JournalEntryLine { AccountId = inventoryAccount.Id, Debit = amount, Credit = 0, Description = "Inventory" });
        entry.Lines.Add(new JournalEntryLine { AccountId = apAccount.Id, Debit = 0, Credit = amount, Description = "Accounts Payable" });

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task ReverseJournalEntry(Guid journalEntryId, string reason)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var original = await _context.JournalEntries
            .Include(j => j.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(j => j.Id == journalEntryId)
            ?? throw new EntityNotFoundException("JournalEntry", journalEntryId);

        if (original.IsReversed)
            throw new DomainException("This journal entry has already been reversed.");

        var reversal = new JournalEntry
        {
            Reference = NumberGenerator.GenerateJournalReference(),
            EntryDate = DateTime.UtcNow,
            Description = $"Reversal of {original.Reference}: {reason}",
            IsAutoPosted = true,
            ReversalOfId = original.Id
        };

        foreach (var line in original.Lines)
        {
            reversal.Lines.Add(new JournalEntryLine
            {
                AccountId = line.AccountId,
                Debit = line.Credit,
                Credit = line.Debit,
                Description = $"Reversal: {line.Description}"
            });
        }

        original.IsReversed = true;
        _context.JournalEntries.Add(reversal);
        await _context.SaveChangesAsync();
    }

    public async Task<TrialBalanceDto> GetTrialBalance(DateTime asOfDate)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var accounts = await _context.Accounts.ToListAsync();
        var lines = new List<TrialBalanceLineDto>();

        foreach (var account in accounts)
        {
            var journalLines = await _context.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == account.Id && l.JournalEntry.EntryDate <= asOfDate)
                .ToListAsync();

            var debit = journalLines.Sum(l => l.Debit);
            var credit = journalLines.Sum(l => l.Credit);

            if (debit != 0 || credit != 0)
            {
                lines.Add(new TrialBalanceLineDto(account.Code, account.Name, account.AccountType, debit, credit));
            }
        }

        return new TrialBalanceDto(asOfDate, lines, lines.Sum(l => l.Debit), lines.Sum(l => l.Credit));
    }

    public async Task<ProfitAndLossDto> GetProfitAndLoss(DateTime from, DateTime to)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var journalLines = await _context.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.EntryDate >= from && l.JournalEntry.EntryDate <= to)
            .ToListAsync();

        var revenueLines = journalLines
            .Where(l => l.Account.AccountType == AccountType.Revenue)
            .GroupBy(l => new { l.Account.Code, l.Account.Name })
            .Select(g => new PnlLineDto(g.Key.Code, g.Key.Name, g.Sum(l => l.Credit - l.Debit)))
            .ToList();

        var expenseLines = journalLines
            .Where(l => l.Account.AccountType == AccountType.Expense)
            .GroupBy(l => new { l.Account.Code, l.Account.Name })
            .Select(g => new PnlLineDto(g.Key.Code, g.Key.Name, g.Sum(l => l.Debit - l.Credit)))
            .ToList();

        var totalRevenue = revenueLines.Sum(l => l.Amount);
        var totalExpenses = expenseLines.Sum(l => l.Amount);

        return new ProfitAndLossDto(from, to, totalRevenue, totalExpenses, totalRevenue - totalExpenses,
            revenueLines, expenseLines);
    }

    public async Task<BalanceSheetDto> GetBalanceSheet(DateTime asOfDate)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var journalLines = await _context.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.EntryDate <= asOfDate)
            .ToListAsync();

        BsLineDto MapLine(IGrouping<(string Code, string Name), JournalEntryLine> g, AccountType type)
        {
            var balance = type is AccountType.Asset or AccountType.Expense
                ? g.Sum(l => l.Debit - l.Credit)
                : g.Sum(l => l.Credit - l.Debit);
            return new BsLineDto(g.Key.Code, g.Key.Name, balance);
        }

        var grouped = journalLines.GroupBy(l => (l.Account.Code, l.Account.Name, l.Account.AccountType));

        var assets = grouped.Where(g => g.Key.AccountType == AccountType.Asset)
            .Select(g => new BsLineDto(g.Key.Code, g.Key.Name, g.Sum(l => l.Debit - l.Credit))).ToList();
        var liabilities = grouped.Where(g => g.Key.AccountType == AccountType.Liability)
            .Select(g => new BsLineDto(g.Key.Code, g.Key.Name, g.Sum(l => l.Credit - l.Debit))).ToList();
        var equity = grouped.Where(g => g.Key.AccountType == AccountType.Equity)
            .Select(g => new BsLineDto(g.Key.Code, g.Key.Name, g.Sum(l => l.Credit - l.Debit))).ToList();

        // Add retained earnings (revenue - expense) to equity
        var revenue = journalLines.Where(l => l.Account.AccountType == AccountType.Revenue).Sum(l => l.Credit - l.Debit);
        var expenses = journalLines.Where(l => l.Account.AccountType == AccountType.Expense).Sum(l => l.Debit - l.Credit);
        equity.Add(new BsLineDto("RE", "Retained Earnings", revenue - expenses));

        return new BalanceSheetDto(asOfDate, assets.Sum(a => a.Amount), liabilities.Sum(l => l.Amount),
            equity.Sum(e => e.Amount), assets, liabilities, equity);
    }

    public async Task<PagedResult<JournalEntryDto>> GetJournalEntries(string? search, int page, int pageSize)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var query = _context.JournalEntries
            .Include(j => j.Lines).ThenInclude(l => l.Account)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(j => j.Reference.Contains(search) || (j.Description != null && j.Description.Contains(search)));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(j => j.EntryDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<JournalEntryDto>(_mapper.Map<List<JournalEntryDto>>(items), total, page, pageSize);
    }

    public async Task<PagedResult<AccountModelDto>> GetAccounts(string? search, AccountType? type, int page, int pageSize)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var query = _context.Accounts.Include(a => a.ParentAccount).AsQueryable();

        if (type.HasValue) query = query.Where(a => a.AccountType == type.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Code.Contains(search) || a.Name.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderBy(a => a.Code)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var dtos = new List<AccountModelDto>();
        foreach (var account in items)
        {
            var balance = await GetAccountBalance(account.Id, _context);
            var dto = _mapper.Map<AccountModelDto>(account);
            dto.Balance = balance;
            dtos.Add(dto);
        }

        return new PagedResult<AccountModelDto>(dtos, total, page, pageSize);
    }

    public async Task<List<AccountModelDto>> GetChartOfAccounts()
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var accounts = await _context.Accounts
            .Include(a => a.ParentAccount)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        var dtos = new List<AccountModelDto>();
        foreach (var account in accounts)
        {
            var balance = await GetAccountBalance(account.Id, _context);            
            var dto = _mapper.Map<AccountModelDto>(account);
            dto.Balance = balance;
            dtos.Add(dto);
        }
        return dtos;
    }

    public async Task<Guid> CreateAccount(AccountModelDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var exists = await _context.Accounts.AnyAsync(a => a.Code == dto.Code);
        if (exists) throw new DomainException($"Account with code {dto.Code} already exists.");

        var account = new Account
        {
            Code = dto.Code,
            Name = dto.Name,
            AccountType = dto.AccountType,
            ParentAccountId = dto.ParentAccountId,
            Description = dto.Description
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account.Id;
    }

    public async Task RecordPayment(CreatePaymentDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var invoice = await _context.Invoices.FindAsync(dto.InvoiceId)
            ?? throw new EntityNotFoundException("Invoice", dto.InvoiceId);

        if (dto.Amount <= 0) throw new DomainException("Payment amount must be positive.");
        if (dto.Amount > invoice.BalanceDue) throw new DomainException("Payment amount exceeds balance due.");

        var payment = new Payment
        {
            PaymentNumber = NumberGenerator.GenerateOrderNumber("PAY"),
            PaymentType = invoice.InvoiceType == InvoiceType.Sales ? PaymentType.Receipt : PaymentType.Payment,
            InvoiceId = dto.InvoiceId,
            CustomerId = invoice.CustomerId,
            SupplierId = invoice.SupplierId,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate,
            PaymentMethod = dto.Method,
            Reference = dto.Reference,
            Notes = dto.Notes
        };

        invoice.PaidAmount += dto.Amount;
        if (invoice.BalanceDue <= 0)
            invoice.Status = InvoiceStatus.Paid;
        else if (invoice.PaidAmount > 0)
            invoice.Status = InvoiceStatus.PartiallyPaid;

        _context.Payments.Add(payment);

        // Auto-post payment journal entry
        var cashAccount = await GetAccountByCode(AccountCodes.Cash, _context);

        if (invoice.InvoiceType == InvoiceType.Sales)
        {
            var arAccount = await GetAccountByCode(AccountCodes.AccountsReceivable, _context);
            var je = new JournalEntry
            {
                Reference = NumberGenerator.GenerateJournalReference(),
                EntryDate = DateTime.UtcNow,
                Description = $"Payment received for {invoice.InvoiceNumber}",
                IsAutoPosted = true
            };
            je.Lines.Add(new JournalEntryLine { AccountId = cashAccount.Id, Debit = dto.Amount, Credit = 0 });
            je.Lines.Add(new JournalEntryLine { AccountId = arAccount.Id, Debit = 0, Credit = dto.Amount });
            _context.JournalEntries.Add(je);
        }
        else
        {
            var apAccount = await GetAccountByCode(AccountCodes.AccountsPayable, _context);
            var je = new JournalEntry
            {
                Reference = NumberGenerator.GenerateJournalReference(),
                EntryDate = DateTime.UtcNow,
                Description = $"Payment made for {invoice.InvoiceNumber}",
                IsAutoPosted = true
            };
            je.Lines.Add(new JournalEntryLine { AccountId = apAccount.Id, Debit = dto.Amount, Credit = 0 });
            je.Lines.Add(new JournalEntryLine { AccountId = cashAccount.Id, Debit = 0, Credit = dto.Amount });
            _context.JournalEntries.Add(je);
        }

        await _context.SaveChangesAsync();
    }

    public async Task RecordPayment(RecordPaymentDto dto)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        if (dto.Amount <= 0) throw new DomainException("Payment amount must be positive.");

        //     public Guid Id { get; set; }
        //public Guid? InvoiceId { get; set; }
        //public PaymentType PaymentType { get; set; }
        //public DateTime? PaymentDate { get; set; }
        //public ControlItemsDto? SelectedParty { get; set; }
        //public PaymentMethod Method { get; set; }
        //public string? PaymentMediaName { get; set; } // for credit card, check and bank transfer
        //public string? AccountName { get; set; }
        //public string? AccountNumber { get; set; }
        //public string? CheckOrCardNumber { get; set; }
        //public string? Reference { get; set; }
        //public decimal Amount { get; set; }
        //public string? Notes { get; set; }

        var payment = new Payment
        {
            PaymentNumber = NumberGenerator.GenerateCode("Pay"),
            PaymentType = dto.PaymentType,
            InvoiceId = dto.InvoiceId,
            CustomerId = dto.CustomerId,
            SupplierId = dto.SupplierId,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate,
            PaymentMethod = dto.PaymentMethod,
            PaymentMediaName = dto.PaymentMediaName,
            AccountName = dto.AccountName,
            AccountNumber = dto.AccountNumber,
            CheckOrCardNumber = dto.CheckOrCardNumber,
            Reference = dto.Reference,
            Notes = dto.Notes
        };

        if (dto.InvoiceId.HasValue)
        {
            var invoice = await _context.Invoices.FindAsync(dto.InvoiceId.Value);
            if (invoice != null)
            {
                invoice.PaidAmount += dto.Amount;
                if (invoice.BalanceDue <= 0)
                    invoice.Status = InvoiceStatus.Paid;
                else if (invoice.PaidAmount > 0)
                    invoice.Status = InvoiceStatus.PartiallyPaid;
            }
        }

        _context.Payments.Add(payment);

        var cashAccount = await GetAccountByCode(AccountCodes.Cash, _context);
        var je = new JournalEntry
        {
            Reference = NumberGenerator.GenerateJournalReference(),
            EntryDate = DateTime.UtcNow,
            Description = $"{dto.PaymentType}: {payment.PaymentNumber}",
            IsAutoPosted = true
        };

        if (dto.PaymentType == PaymentType.Receipt)
        {
            var arAccount = await GetAccountByCode(AccountCodes.AccountsReceivable, _context);
            je.Lines.Add(new JournalEntryLine { AccountId = cashAccount.Id, Debit = dto.Amount, Credit = 0 });
            je.Lines.Add(new JournalEntryLine { AccountId = arAccount.Id, Debit = 0, Credit = dto.Amount });
        }
        else
        {
            var apAccount = await GetAccountByCode(AccountCodes.AccountsPayable, _context);
            je.Lines.Add(new JournalEntryLine { AccountId = apAccount.Id, Debit = dto.Amount, Credit = 0 });
            je.Lines.Add(new JournalEntryLine { AccountId = cashAccount.Id, Debit = 0, Credit = dto.Amount });
        }

        _context.JournalEntries.Add(je);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AgingReportDto>> GetArAging()
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.InvoiceType == InvoiceType.Sales && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync();

        return invoices.Where(i => i.Customer != null)
            .GroupBy(i => new { i.CustomerId, i.Customer!.CustomerName })
            .Select(g =>
            {
                var current = g.Where(i => i.DueDate >= DateTime.UtcNow).Sum(i => i.BalanceDue);
                var d1to30 = g.Where(i => i.DueDate < DateTime.UtcNow && i.DueDate >= DateTime.UtcNow.AddDays(-30)).Sum(i => i.BalanceDue);
                var d31to60 = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-30) && i.DueDate >= DateTime.UtcNow.AddDays(-60)).Sum(i => i.BalanceDue);
                var d61to90 = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-60) && i.DueDate >= DateTime.UtcNow.AddDays(-90)).Sum(i => i.BalanceDue);
                var d90plus = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-90)).Sum(i => i.BalanceDue);
                return new AgingReportDto(g.Key.CustomerName, g.Key.CustomerId!.Value, current, d1to30, d31to60, d61to90, d90plus, current + d1to30 + d31to60 + d61to90 + d90plus);
            }).ToList();
    }

    public async Task<List<AgingReportDto>> GetApAging()
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var invoices = await _context.Invoices
            .Include(i => i.Supplier)
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync();

        return invoices.Where(i => i.Supplier != null)
            .GroupBy(i => new { i.SupplierId, i.Supplier!.SupplierName })
            .Select(g =>
            {
                var current = g.Where(i => i.DueDate >= DateTime.UtcNow).Sum(i => i.BalanceDue);
                var d1to30 = g.Where(i => i.DueDate < DateTime.UtcNow && i.DueDate >= DateTime.UtcNow.AddDays(-30)).Sum(i => i.BalanceDue);
                var d31to60 = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-30) && i.DueDate >= DateTime.UtcNow.AddDays(-60)).Sum(i => i.BalanceDue);
                var d61to90 = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-60) && i.DueDate >= DateTime.UtcNow.AddDays(-90)).Sum(i => i.BalanceDue);
                var d90plus = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-90)).Sum(i => i.BalanceDue);
                return new AgingReportDto(g.Key.SupplierName, g.Key.SupplierId!.Value, current, d1to30, d31to60, d61to90, d90plus, current + d1to30 + d31to60 + d61to90 + d90plus);
            }).ToList();
    }

    private static async Task<Account> GetAccountByCode(string code, ApplicationDbContext _context)
    {
        return await _context.Accounts.FirstOrDefaultAsync(a => a.Code == code)
            ?? throw new DomainException($"System account {code} not found. Please run seed data.");
    }

    /// <summary>
    /// Journal lines for the ledger screen. Replaces a query the page ran against its own scoped
    /// context, which had to include the entry and account graphs before it could project.
    /// </summary>
    public async Task<List<GeneralLedgerLineDto>> GetGeneralLedgerLines(Guid? accountId, DateTime? from, DateTime? to)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var query = _context.JournalEntryLines.AsQueryable();

        if (accountId.HasValue)
            query = query.Where(l => l.AccountId == accountId.Value);
        if (from.HasValue)
            query = query.Where(l => l.JournalEntry.EntryDate >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.JournalEntry.EntryDate < to.Value.Date.AddDays(1));

        return await query
            .OrderBy(l => l.JournalEntry.EntryDate)
            .ThenBy(l => l.JournalEntry.Reference)
            .Select(l => new GeneralLedgerLineDto(
                l.JournalEntry.EntryDate,
                l.JournalEntry.Reference,
                l.Account.Code,
                l.Account.Name,
                l.JournalEntry.Description,
                l.Debit,
                l.Credit))
            .ToListAsync();
    }

    public async Task<List<PaymentListItemDto>> GetRecentPayments(int count)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        return await _context.Payments
            .AsNoTracking()
            .OrderByDescending(p => p.PaymentDate)
            .Take(count)
            .Select(p => new PaymentListItemDto(
                p.Id,
                p.PaymentNumber,
                p.PaymentDate,
                p.PaymentType,
                p.PaymentMethod,
                p.Status,
                p.Amount,
                p.Reference,
                p.Notes,
                p.PaymentMediaName,
                p.AccountName,
                p.AccountNumber,
                p.CheckOrCardNumber,
                p.InvoiceId,
                p.Invoice != null ? p.Invoice.InvoiceNumber : null,
                p.Customer != null ? p.Customer.CustomerName : p.Supplier != null ? p.Supplier.SupplierName : ""))
            .ToListAsync();
    }

    public async Task<CustomerLedgerDto> GetCustomerLedger(Guid? customerId, DateTime? from, DateTime? to)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var fromDate = from?.Date;
        var toExclusive = to?.Date.AddDays(1);

        var invoiceQuery = context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Sales
                        && i.CustomerId != null
                        && i.Status != InvoiceStatus.Cancelled);
        var paymentQuery = context.Payments.AsNoTracking()
            .Where(p => p.CustomerId != null
                        && p.PaymentType == PaymentType.Receipt
                        && p.Status == PaymentStatus.Completed);
        var creditQuery = context.CreditNotes.AsNoTracking()
            .Where(c => (c.Status == CreditNoteStatus.Approved || c.Status == CreditNoteStatus.Applied)
                        && c.Invoice.InvoiceType == InvoiceType.Sales
                        && c.Invoice.CustomerId != null);

        if (customerId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(i => i.CustomerId == customerId.Value);
            paymentQuery = paymentQuery.Where(p => p.CustomerId == customerId.Value);
            creditQuery = creditQuery.Where(c => c.Invoice.CustomerId == customerId.Value);
        }

        var customerNames = await context.Customers.AsNoTracking()
            .Where(c => !customerId.HasValue || c.Id == customerId.Value)
            .Select(c => new { c.Id, c.CustomerName })
            .ToDictionaryAsync(c => c.Id, c => c.CustomerName);

        var windowInvoices = invoiceQuery;
        var windowPayments = paymentQuery;
        var windowCredits = creditQuery;
        if (fromDate.HasValue)
        {
            windowInvoices = windowInvoices.Where(i => i.InvoiceDate >= fromDate.Value);
            windowPayments = windowPayments.Where(p => p.PaymentDate >= fromDate.Value);
            windowCredits = windowCredits.Where(c => c.Date >= fromDate.Value);
        }
        if (toExclusive.HasValue)
        {
            windowInvoices = windowInvoices.Where(i => i.InvoiceDate < toExclusive.Value);
            windowPayments = windowPayments.Where(p => p.PaymentDate < toExclusive.Value);
            windowCredits = windowCredits.Where(c => c.Date < toExclusive.Value);
        }

        var raw = new List<(DateTime Date, int SortOrder, Guid CustomerId, string Type, string Reference, decimal Debit, decimal Credit)>();

        foreach (var invoice in await windowInvoices
            .Select(i => new { i.InvoiceDate, CustomerId = i.CustomerId!.Value, i.InvoiceNumber, i.TotalAmount })
            .ToListAsync())
        {
            raw.Add((invoice.InvoiceDate, 0, invoice.CustomerId, "Sales Invoice", invoice.InvoiceNumber, invoice.TotalAmount, 0m));
        }

        foreach (var payment in await windowPayments
            .Select(p => new { p.PaymentDate, CustomerId = p.CustomerId!.Value, p.PaymentNumber, p.InvoiceId, p.Amount })
            .ToListAsync())
        {
            var direct = !payment.InvoiceId.HasValue;
            raw.Add((payment.PaymentDate, direct ? 3 : 2, payment.CustomerId,
                direct ? "Direct Payment" : "Payment", payment.PaymentNumber, 0m, payment.Amount));
        }

        foreach (var credit in await windowCredits
            .Select(c => new { c.Date, CustomerId = c.Invoice.CustomerId!.Value, c.CreditNoteNumber, c.Amount })
            .ToListAsync())
        {
            raw.Add((credit.Date, 1, credit.CustomerId, "Credit Note", credit.CreditNoteNumber, 0m, credit.Amount));
        }

        var lines = new List<CustomerLedgerLineDto>();
        foreach (var group in raw.GroupBy(r => r.CustomerId))
        {
            var balance = 0m;
            foreach (var entry in group.OrderBy(r => r.Date).ThenBy(r => r.SortOrder).ThenBy(r => r.Reference))
            {
                balance += entry.Debit - entry.Credit;
                lines.Add(new CustomerLedgerLineDto(
                    entry.CustomerId,
                    customerNames.GetValueOrDefault(entry.CustomerId, "Customer"),
                    entry.Date,
                    entry.Type,
                    entry.Reference,
                    entry.Debit,
                    entry.Credit,
                    balance));
            }
        }

        var orderedLines = lines
            .OrderBy(l => l.Date)
            .ThenBy(l => l.CustomerName)
            .ThenBy(l => l.Reference)
            .ToList();
        var totalDebits = orderedLines.Sum(l => l.Debit);
        var totalCredits = orderedLines.Sum(l => l.Credit);

        return new CustomerLedgerDto(fromDate, to?.Date, orderedLines,
            totalDebits, totalCredits, totalDebits - totalCredits);
    }

    public async Task<SupplierLedgerDto> GetSupplierLedger(Guid? supplierId, DateTime? from, DateTime? to)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var fromDate = from?.Date;
        var toExclusive = to?.Date.AddDays(1);

        var invoiceQuery = context.Invoices.AsNoTracking()
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && i.SupplierId != null
                        && i.Status != InvoiceStatus.Cancelled);
        var paymentQuery = context.Payments.AsNoTracking()
            .Where(p => p.SupplierId != null
                        && p.PaymentType == PaymentType.Payment
                        && p.Status == PaymentStatus.Completed);
        var debitQuery = context.DebitNotes.AsNoTracking()
            .Where(d => (d.Status == DebitNoteStatus.Approved || d.Status == DebitNoteStatus.Applied)
                        && d.Invoice.InvoiceType == InvoiceType.Purchase
                        && d.Invoice.SupplierId != null);

        if (supplierId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(i => i.SupplierId == supplierId.Value);
            paymentQuery = paymentQuery.Where(p => p.SupplierId == supplierId.Value);
            debitQuery = debitQuery.Where(d => d.Invoice.SupplierId == supplierId.Value);
        }

        var supplierNames = await context.Suppliers.AsNoTracking()
            .Where(s => !supplierId.HasValue || s.Id == supplierId.Value)
            .Select(s => new { s.Id, s.SupplierName })
            .ToDictionaryAsync(s => s.Id, s => s.SupplierName);

        var windowInvoices = invoiceQuery;
        var windowPayments = paymentQuery;
        var windowDebits = debitQuery;
        if (fromDate.HasValue)
        {
            windowInvoices = windowInvoices.Where(i => i.InvoiceDate >= fromDate.Value);
            windowPayments = windowPayments.Where(p => p.PaymentDate >= fromDate.Value);
            windowDebits = windowDebits.Where(d => d.Date >= fromDate.Value);
        }
        if (toExclusive.HasValue)
        {
            windowInvoices = windowInvoices.Where(i => i.InvoiceDate < toExclusive.Value);
            windowPayments = windowPayments.Where(p => p.PaymentDate < toExclusive.Value);
            windowDebits = windowDebits.Where(d => d.Date < toExclusive.Value);
        }

        var raw = new List<(DateTime Date, int SortOrder, Guid SupplierId, string Type, string Reference, decimal Debit, decimal Credit)>();

        foreach (var invoice in await windowInvoices
            .Select(i => new { i.InvoiceDate, SupplierId = i.SupplierId!.Value, i.InvoiceNumber, i.TotalAmount })
            .ToListAsync())
        {
            raw.Add((invoice.InvoiceDate, 0, invoice.SupplierId, "Purchase Invoice", invoice.InvoiceNumber, 0m, invoice.TotalAmount));
        }

        foreach (var payment in await windowPayments
            .Select(p => new { p.PaymentDate, SupplierId = p.SupplierId!.Value, p.PaymentNumber, p.InvoiceId, p.Amount })
            .ToListAsync())
        {
            var direct = !payment.InvoiceId.HasValue;
            raw.Add((payment.PaymentDate, direct ? 3 : 2, payment.SupplierId,
                direct ? "Direct Payment" : "Payment", payment.PaymentNumber, payment.Amount, 0m));
        }

        foreach (var debit in await windowDebits
            .Select(d => new { d.Date, SupplierId = d.Invoice.SupplierId!.Value, d.DebitNoteNumber, d.Amount })
            .ToListAsync())
        {
            raw.Add((debit.Date, 1, debit.SupplierId, "Debit Note", debit.DebitNoteNumber, debit.Amount, 0m));
        }

        var lines = new List<SupplierLedgerLineDto>();
        foreach (var group in raw.GroupBy(r => r.SupplierId))
        {
            var balance = 0m;
            foreach (var entry in group.OrderBy(r => r.Date).ThenBy(r => r.SortOrder).ThenBy(r => r.Reference))
            {
                balance += entry.Credit - entry.Debit;
                lines.Add(new SupplierLedgerLineDto(
                    entry.SupplierId,
                    supplierNames.GetValueOrDefault(entry.SupplierId, "Supplier"),
                    entry.Date,
                    entry.Type,
                    entry.Reference,
                    entry.Debit,
                    entry.Credit,
                    balance));
            }
        }

        var orderedLines = lines
            .OrderBy(l => l.Date)
            .ThenBy(l => l.SupplierName)
            .ThenBy(l => l.Reference)
            .ToList();
        var totalDebits = orderedLines.Sum(l => l.Debit);
        var totalCredits = orderedLines.Sum(l => l.Credit);

        return new SupplierLedgerDto(fromDate, to?.Date, orderedLines,
            totalDebits, totalCredits, totalCredits - totalDebits);
    }

    /// <summary>
    /// Invoices and receipts for one customer over a window. The statement screen ran both
    /// queries against its own context; the running-balance and chart work stays in the page.
    /// </summary>
    public async Task<CustomerStatementDto> GetCustomerStatement(Guid customerId, DateTime from, DateTime to)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var invoices = await _context.Invoices
            .Where(i => i.CustomerId == customerId && i.InvoiceDate >= from && i.InvoiceDate < to)
            .OrderBy(i => i.InvoiceDate)
            .AsNoTracking()
            .Select(i => new StatementInvoiceRowDto(i.Id, i.InvoiceNumber, i.InvoiceDate, i.TotalAmount, i.PaidAmount, i.IsInitialInvoice))
            .ToListAsync();

        var payments = await _context.Payments
            .Where(p => p.CustomerId == customerId && p.PaymentDate >= from && p.PaymentDate < to)
            .OrderBy(p => p.PaymentDate)
            .AsNoTracking()
            .Select(p => new StatementPaymentRowDto(p.Id, p.PaymentNumber, p.PaymentDate, p.Amount))
            .ToListAsync();

        return new CustomerStatementDto(invoices, payments);
    }
    public async Task<CustomerStatementDto> GetSupplierStatement(Guid supplierId, DateTime from, DateTime to)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();

        var invoices = await _context.Invoices
            .Where(i => i.SupplierId == supplierId && i.InvoiceDate >= from && i.InvoiceDate < to)
            .OrderBy(i => i.InvoiceDate)
            .AsNoTracking()
            .Select(i => new StatementInvoiceRowDto(i.Id, i.InvoiceNumber, i.InvoiceDate, i.TotalAmount, i.PaidAmount, i.IsInitialInvoice))
            .ToListAsync();

        var payments = await _context.Payments
            .Where(p => p.SupplierId == supplierId && p.PaymentDate >= from && p.PaymentDate < to)
            .OrderBy(p => p.PaymentDate)
            .AsNoTracking()
            .Select(p => new StatementPaymentRowDto(p.Id, p.PaymentNumber, p.PaymentDate, p.Amount))
            .ToListAsync();

        return new CustomerStatementDto(invoices, payments);
    }

    private static async Task<decimal> GetAccountBalance(Guid accountId, ApplicationDbContext _context)
    {
        var lines = await _context.JournalEntryLines
            .Where(l => l.AccountId == accountId)
            .ToListAsync();

        var account = await _context.Accounts.FindAsync(accountId);
        if (account == null) return 0;

        return account.AccountType is AccountType.Asset or AccountType.Expense
            ? lines.Sum(l => l.Debit - l.Credit)
            : lines.Sum(l => l.Credit - l.Debit);
    }
}
