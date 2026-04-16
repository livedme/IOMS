using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Domain.Exceptions;
using IOMS.Infrastructure.Data;
using IOMS.Shared.Constants;
using IOMS.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services;

public class AccountingService : IAccountingService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public AccountingService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Guid> CreateJournalEntry(CreateJournalEntryDto dto)
    {
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
        var arAccount = await GetAccountByCode(AccountCodes.AccountsReceivable);
        var revenueAccount = await GetAccountByCode(AccountCodes.SalesRevenue);

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
        var inventoryAccount = await GetAccountByCode(AccountCodes.Inventory);
        var apAccount = await GetAccountByCode(AccountCodes.AccountsPayable);

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

    public async Task<PagedResult<AccountDto>> GetAccounts(string? search, AccountType? type, int page, int pageSize)
    {
        var query = _context.Accounts.Include(a => a.ParentAccount).AsQueryable();

        if (type.HasValue) query = query.Where(a => a.AccountType == type.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Code.Contains(search) || a.Name.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderBy(a => a.Code)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var dtos = new List<AccountDto>();
        foreach (var account in items)
        {
            var balance = await GetAccountBalance(account.Id);
            var dto = _mapper.Map<AccountDto>(account);
            dtos.Add(dto with { Balance = balance });
        }

        return new PagedResult<AccountDto>(dtos, total, page, pageSize);
    }

    public async Task<List<AccountDto>> GetChartOfAccounts()
    {
        var accounts = await _context.Accounts
            .Include(a => a.ParentAccount)
            .OrderBy(a => a.Code)
            .ToListAsync();

        var dtos = new List<AccountDto>();
        foreach (var account in accounts)
        {
            var balance = await GetAccountBalance(account.Id);
            var dto = _mapper.Map<AccountDto>(account);
            dtos.Add(dto with { Balance = balance });
        }
        return dtos;
    }

    public async Task<Guid> CreateAccount(CreateAccountDto dto)
    {
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
        var cashAccount = await GetAccountByCode(AccountCodes.Cash);

        if (invoice.InvoiceType == InvoiceType.Sales)
        {
            var arAccount = await GetAccountByCode(AccountCodes.AccountsReceivable);
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
            var apAccount = await GetAccountByCode(AccountCodes.AccountsPayable);
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
        if (dto.Amount <= 0) throw new DomainException("Payment amount must be positive.");

        var payment = new Payment
        {
            PaymentNumber = NumberGenerator.GenerateOrderNumber("PAY"),
            PaymentType = dto.PaymentType,
            InvoiceId = dto.InvoiceId,
            CustomerId = dto.CustomerId,
            SupplierId = dto.SupplierId,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate,
            PaymentMethod = dto.PaymentMethod,
            Reference = dto.Reference
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

        var cashAccount = await GetAccountByCode(AccountCodes.Cash);
        var je = new JournalEntry
        {
            Reference = NumberGenerator.GenerateJournalReference(),
            EntryDate = DateTime.UtcNow,
            Description = $"{dto.PaymentType}: {payment.PaymentNumber}",
            IsAutoPosted = true
        };

        if (dto.PaymentType == PaymentType.Receipt)
        {
            var arAccount = await GetAccountByCode(AccountCodes.AccountsReceivable);
            je.Lines.Add(new JournalEntryLine { AccountId = cashAccount.Id, Debit = dto.Amount, Credit = 0 });
            je.Lines.Add(new JournalEntryLine { AccountId = arAccount.Id, Debit = 0, Credit = dto.Amount });
        }
        else
        {
            var apAccount = await GetAccountByCode(AccountCodes.AccountsPayable);
            je.Lines.Add(new JournalEntryLine { AccountId = apAccount.Id, Debit = dto.Amount, Credit = 0 });
            je.Lines.Add(new JournalEntryLine { AccountId = cashAccount.Id, Debit = 0, Credit = dto.Amount });
        }

        _context.JournalEntries.Add(je);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AgingReportDto>> GetArAging()
    {
        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.InvoiceType == InvoiceType.Sales && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync();

        return invoices.Where(i => i.Customer != null)
            .GroupBy(i => new { i.CustomerId, i.Customer!.Name })
            .Select(g =>
            {
                var current = g.Where(i => i.DueDate >= DateTime.UtcNow).Sum(i => i.BalanceDue);
                var d1to30 = g.Where(i => i.DueDate < DateTime.UtcNow && i.DueDate >= DateTime.UtcNow.AddDays(-30)).Sum(i => i.BalanceDue);
                var d31to60 = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-30) && i.DueDate >= DateTime.UtcNow.AddDays(-60)).Sum(i => i.BalanceDue);
                var d61to90 = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-60) && i.DueDate >= DateTime.UtcNow.AddDays(-90)).Sum(i => i.BalanceDue);
                var d90plus = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-90)).Sum(i => i.BalanceDue);
                return new AgingReportDto(g.Key.Name, g.Key.CustomerId!.Value, current, d1to30, d31to60, d61to90, d90plus, current + d1to30 + d31to60 + d61to90 + d90plus);
            }).ToList();
    }

    public async Task<List<AgingReportDto>> GetApAging()
    {
        var invoices = await _context.Invoices
            .Include(i => i.Supplier)
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync();

        return invoices.Where(i => i.Supplier != null)
            .GroupBy(i => new { i.SupplierId, i.Supplier!.Name })
            .Select(g =>
            {
                var current = g.Where(i => i.DueDate >= DateTime.UtcNow).Sum(i => i.BalanceDue);
                var d1to30 = g.Where(i => i.DueDate < DateTime.UtcNow && i.DueDate >= DateTime.UtcNow.AddDays(-30)).Sum(i => i.BalanceDue);
                var d31to60 = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-30) && i.DueDate >= DateTime.UtcNow.AddDays(-60)).Sum(i => i.BalanceDue);
                var d61to90 = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-60) && i.DueDate >= DateTime.UtcNow.AddDays(-90)).Sum(i => i.BalanceDue);
                var d90plus = g.Where(i => i.DueDate < DateTime.UtcNow.AddDays(-90)).Sum(i => i.BalanceDue);
                return new AgingReportDto(g.Key.Name, g.Key.SupplierId!.Value, current, d1to30, d31to60, d61to90, d90plus, current + d1to30 + d31to60 + d61to90 + d90plus);
            }).ToList();
    }

    private async Task<Account> GetAccountByCode(string code)
    {
        return await _context.Accounts.FirstOrDefaultAsync(a => a.Code == code)
            ?? throw new DomainException($"System account {code} not found. Please run seed data.");
    }

    private async Task<decimal> GetAccountBalance(Guid accountId)
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
