using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Domain.Exceptions;
using IOMS.Infrastructure.Data;
using IOMS.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services;

public class RfqService : IRfqService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public RfqService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<RfqRequestDto>> GetRfqRequests(string? search, RfqStatus? status, int page, int pageSize)
    {
        var query = _context.RfqRequests
            .AsSplitQuery()
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .Include(r => r.SupplierResponses).ThenInclude(sr => sr.Supplier)
            .AsQueryable();

        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.RfqNumber.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<RfqRequestDto>(_mapper.Map<List<RfqRequestDto>>(items), total, page, pageSize);
    }

    public async Task<RfqRequestDto> GetRfqRequestById(Guid id)
    {
        var rfq = await _context.RfqRequests
            .AsSplitQuery()
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .Include(r => r.SupplierResponses).ThenInclude(sr => sr.Supplier)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new EntityNotFoundException("RfqRequest", id);
        return _mapper.Map<RfqRequestDto>(rfq);
    }

    public async Task<Guid> CreateRfqRequest(CreateRfqRequestDto dto)
    {
        var rfq = new RfqRequest
        {
            RfqNumber = NumberGenerator.GenerateOrderNumber("RFQ"),
            RequiredDate = dto.RequiredDate,
            Notes = dto.Notes,
            Status = RfqStatus.Draft
        };

        foreach (var item in dto.Items)
        {
            rfq.Items.Add(new RfqItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                TargetUnitPrice = item.TargetUnitPrice
            });
        }

        _context.RfqRequests.Add(rfq);
        await _context.SaveChangesAsync();
        return rfq.Id;
    }

    public async Task AddSupplierResponse(CreateRfqSupplierResponseDto dto)
    {
        var rfq = await _context.RfqRequests.FindAsync(dto.RfqRequestId)
            ?? throw new EntityNotFoundException("RfqRequest", dto.RfqRequestId);

        _context.RfqSupplierResponses.Add(new RfqSupplierResponse
        {
            RfqRequestId = dto.RfqRequestId,
            SupplierId = dto.SupplierId,
            QuotedPrice = dto.QuotedPrice,
            LeadTimeDays = dto.LeadTimeDays,
            ValidUntil = dto.ValidUntil,
            Notes = dto.Notes
        });

        if (rfq.Status == RfqStatus.Sent)
            rfq.Status = RfqStatus.Received;

        await _context.SaveChangesAsync();
    }

    public async Task AwardRfq(Guid rfqId, Guid supplierResponseId)
    {
        var rfq = await _context.RfqRequests
            .Include(r => r.SupplierResponses)
            .FirstOrDefaultAsync(r => r.Id == rfqId)
            ?? throw new EntityNotFoundException("RfqRequest", rfqId);

        foreach (var resp in rfq.SupplierResponses)
            resp.IsSelected = resp.Id == supplierResponseId;

        rfq.Status = RfqStatus.Awarded;
        await _context.SaveChangesAsync();
    }

    public async Task<Guid> ConvertRfqToPurchaseOrder(Guid rfqId, Guid supplierResponseId)
    {
        var rfq = await _context.RfqRequests
            .Include(r => r.Items)
            .Include(r => r.SupplierResponses)
            .FirstOrDefaultAsync(r => r.Id == rfqId)
            ?? throw new EntityNotFoundException("RfqRequest", rfqId);

        var response = rfq.SupplierResponses.FirstOrDefault(r => r.Id == supplierResponseId)
            ?? throw new DomainException("Supplier response not found.");

        var po = new PurchaseOrder
        {
            OrderNumber = NumberGenerator.GenerateOrderNumber("PO"),
            SupplierId = response.SupplierId,
            Status = PurchaseOrderStatus.Draft,
            Notes = $"Created from RFQ {rfq.RfqNumber}"
        };

        foreach (var item in rfq.Items)
        {
            po.Items.Add(new PurchaseOrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = response.QuotedPrice,
                LineTotal = item.Quantity * response.QuotedPrice
            });
        }

        po.SubTotal = po.Items.Sum(i => i.LineTotal);
        po.TotalAmount = po.SubTotal + po.TaxAmount;

        rfq.Status = RfqStatus.Closed;
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();
        return po.Id;
    }
}
