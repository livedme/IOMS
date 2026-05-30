using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Domain.Exceptions;
using IOMS.Infrastructure.Data;
using IOMS.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services;

public class CreditDebitNoteService : ICreditDebitNoteService
{
    private readonly ApplicationDbContext _context;

    public CreditDebitNoteService(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<CreditNoteDto>> GetCreditNotes(string? search, int page, int pageSize)
    {
        var query = _context.CreditNotes
            .Include(c => c.Invoice)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.CreditNoteNumber.Contains(search) || c.Invoice.InvoiceNumber.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(c => c.Date)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CreditNoteDto(c.Id, c.CreditNoteNumber, c.InvoiceId,
                c.Invoice.InvoiceNumber, c.Date, c.Amount, c.Reason, c.Status))
            .ToListAsync();

        return new PagedResult<CreditNoteDto>(items, total, page, pageSize);
    }

    public async Task<Guid> CreateCreditNote(CreateCreditNoteDto dto)
    {
        var invoice = await _context.Invoices.FindAsync(dto.InvoiceId)
            ?? throw new EntityNotFoundException("Invoice", dto.InvoiceId);

        var cn = new CreditNote
        {
            CreditNoteNumber = NumberGenerator.GenerateOrderNumber("CN"),
            InvoiceId = dto.InvoiceId,
            Amount = dto.Amount,
            Reason = dto.Reason,
            Status = CreditNoteStatus.Draft
        };

        _context.CreditNotes.Add(cn);
        await _context.SaveChangesAsync();
        return cn.Id;
    }

    public async Task ApproveCreditNote(Guid id)
    {
        var cn = await _context.CreditNotes
            .Include(c => c.Invoice)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new EntityNotFoundException("CreditNote", id);

        cn.Status = CreditNoteStatus.Approved;
        cn.Invoice.PaidAmount += cn.Amount;
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<DebitNoteDto>> GetDebitNotes(string? search, int page, int pageSize)
    {
        var query = _context.DebitNotes
            .Include(d => d.Invoice)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.DebitNoteNumber.Contains(search) || d.Invoice.InvoiceNumber.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(d => d.Date)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new DebitNoteDto(d.Id, d.DebitNoteNumber, d.InvoiceId,
                d.Invoice.InvoiceNumber, d.Date, d.Amount, d.Reason, d.Status))
            .ToListAsync();

        return new PagedResult<DebitNoteDto>(items, total, page, pageSize);
    }

    public async Task<Guid> CreateDebitNote(CreateDebitNoteDto dto)
    {
        var invoice = await _context.Invoices.FindAsync(dto.InvoiceId)
            ?? throw new EntityNotFoundException("Invoice", dto.InvoiceId);

        var dn = new DebitNote
        {
            DebitNoteNumber = NumberGenerator.GenerateOrderNumber("DN"),
            InvoiceId = dto.InvoiceId,
            Amount = dto.Amount,
            Reason = dto.Reason,
            Status = DebitNoteStatus.Draft
        };

        _context.DebitNotes.Add(dn);
        await _context.SaveChangesAsync();
        return dn.Id;
    }

    public async Task ApproveDebitNote(Guid id)
    {
        var dn = await _context.DebitNotes
            .Include(d => d.Invoice)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new EntityNotFoundException("DebitNote", id);

        dn.Status = DebitNoteStatus.Approved;
        dn.Invoice.TotalAmount += dn.Amount;
        await _context.SaveChangesAsync();
    }
}
