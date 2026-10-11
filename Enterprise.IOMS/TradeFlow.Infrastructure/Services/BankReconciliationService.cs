using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TradeFlow.Infrastructure.Services;

public class BankReconciliationService : IBankReconciliationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public BankReconciliationService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<BankStatementDto>> GetBankStatements()
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        return await _context.BankStatements
            .Include(b => b.Lines)
            .OrderByDescending(b => b.StatementDate)
            .Select(b => new BankStatementDto(
                b.Id, b.BankAccountName, b.StatementDate, b.FileName,
                b.ImportedAt, b.IsReconciled,
                b.Lines.Select(l => new BankStatementLineDto(
                    l.Id, l.TransactionDate, l.Description, l.Amount,
                    l.Reference, l.MatchedPaymentId, l.IsMatched)).ToList()))
            .ToListAsync();
    }

    public async Task<BankStatementDto> GetBankStatementById(Guid id)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var bs = await _context.BankStatements
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new EntityNotFoundException("BankStatement", id);

        return new BankStatementDto(bs.Id, bs.BankAccountName, bs.StatementDate, bs.FileName,
            bs.ImportedAt, bs.IsReconciled,
            bs.Lines.Select(l => new BankStatementLineDto(
                l.Id, l.TransactionDate, l.Description, l.Amount,
                l.Reference, l.MatchedPaymentId, l.IsMatched)).ToList());
    }

    public async Task<Guid> ImportBankStatement(CreateBankStatementDto dto, List<CreateBankStatementLineDto> lines)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var bs = new BankStatement
        {
            BankAccountName = dto.BankAccountName,
            StatementDate = dto.StatementDate,
            FileName = dto.FileName
        };

        foreach (var line in lines)
        {
            bs.Lines.Add(new BankStatementLine
            {
                TransactionDate = line.TransactionDate,
                Description = line.Description,
                Amount = line.Amount,
                Reference = line.Reference
            });
        }

        _context.BankStatements.Add(bs);
        await _context.SaveChangesAsync();
        return bs.Id;
    }

    public async Task MatchLine(Guid bankStatementLineId, Guid paymentId)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var line = await _context.BankStatementLines.FindAsync(bankStatementLineId)
            ?? throw new EntityNotFoundException("BankStatementLine", bankStatementLineId);

        var payment = await _context.Payments.FindAsync(paymentId)
            ?? throw new EntityNotFoundException("Payment", paymentId);

        line.MatchedPaymentId = paymentId;
        line.IsMatched = true;
        payment.MatchedBankStatementLineId = bankStatementLineId;
        await _context.SaveChangesAsync();
    }

    public async Task UnmatchLine(Guid bankStatementLineId)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var line = await _context.BankStatementLines.FindAsync(bankStatementLineId)
            ?? throw new EntityNotFoundException("BankStatementLine", bankStatementLineId);

        if (line.MatchedPaymentId.HasValue)
        {
            var payment = await _context.Payments.FindAsync(line.MatchedPaymentId.Value);
            if (payment != null) payment.MatchedBankStatementLineId = null;
        }

        line.MatchedPaymentId = null;
        line.IsMatched = false;
        await _context.SaveChangesAsync();
    }

    public async Task ApproveReconciliation(Guid bankStatementId)
    {
        await using var _context = await _dbFactory.CreateDbContextAsync();
        var bs = await _context.BankStatements.FindAsync(bankStatementId)
            ?? throw new EntityNotFoundException("BankStatement", bankStatementId);
        bs.IsReconciled = true;
        await _context.SaveChangesAsync();
    }
}
