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

public class PurchaseReturnService : IPurchaseReturnService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public PurchaseReturnService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<PurchaseReturnDto>> GetPurchaseReturns(string? search, PurchaseReturnStatus? status, int page, int pageSize)
    {
        var query = _context.PurchaseReturns
            .Include(r => r.PurchaseOrder)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.ReturnNumber.Contains(search) || r.PurchaseOrder.OrderNumber.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<PurchaseReturnDto>(_mapper.Map<List<PurchaseReturnDto>>(items), total, page, pageSize);
    }

    public async Task<PurchaseReturnDto> GetPurchaseReturnById(Guid id)
    {
        var ret = await _context.PurchaseReturns
            .Include(r => r.PurchaseOrder)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new EntityNotFoundException("PurchaseReturn", id);
        return _mapper.Map<PurchaseReturnDto>(ret);
    }

    public async Task<Guid> CreatePurchaseReturn(CreatePurchaseReturnDto dto)
    {
        var po = await _context.PurchaseOrders.FindAsync(dto.PurchaseOrderId)
            ?? throw new EntityNotFoundException("PurchaseOrder", dto.PurchaseOrderId);

        var ret = new PurchaseReturn
        {
            ReturnNumber = NumberGenerator.GenerateOrderNumber("PR"),
            PurchaseOrderId = dto.PurchaseOrderId,
            Reason = dto.Reason,
            Status = PurchaseReturnStatus.Draft
        };

        foreach (var item in dto.Items)
        {
            ret.Items.Add(new PurchaseReturnItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                LineTotal = item.Quantity * item.UnitCost
            });
        }

        ret.TotalAmount = ret.Items.Sum(i => i.LineTotal);
        _context.PurchaseReturns.Add(ret);
        await _context.SaveChangesAsync();
        return ret.Id;
    }

    public async Task ApprovePurchaseReturn(Guid id)
    {
        var ret = await _context.PurchaseReturns
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new EntityNotFoundException("PurchaseReturn", id);

        ret.Status = PurchaseReturnStatus.Approved;

        foreach (var item in ret.Items)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId);
            if (inv != null)
            {
                inv.Quantity -= item.Quantity;
                _context.StockMovements.Add(new StockMovement
                {
                    ProductId = item.ProductId,
                    WarehouseId = inv.WarehouseId,
                    Type = StockMovementType.Return,
                    Quantity = -item.Quantity,
                    MovementDate = DateTime.UtcNow,
                    Reference = ret.ReturnNumber
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task CompletePurchaseReturn(Guid id)
    {
        var ret = await _context.PurchaseReturns.FindAsync(id)
            ?? throw new EntityNotFoundException("PurchaseReturn", id);
        ret.Status = PurchaseReturnStatus.Completed;
        await _context.SaveChangesAsync();
    }
}
