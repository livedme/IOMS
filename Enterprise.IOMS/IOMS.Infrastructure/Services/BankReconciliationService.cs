using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Exceptions;
using IOMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services;

public class BankReconciliationService : IBankReconciliationService
{
    private readonly AppDbContext _context;

    public BankReconciliationService(AppDbContext context) => _context = context;

    public async Task<List<BankStatementDto>> GetBankStatements()
    {
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
        var bs = await _context.BankStatements.FindAsync(bankStatementId)
            ?? throw new EntityNotFoundException("BankStatement", bankStatementId);
        bs.IsReconciled = true;
        await _context.SaveChangesAsync();
    }
}
