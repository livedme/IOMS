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

public class SalesReturnService : ISalesReturnService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public SalesReturnService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<SalesReturnDto>> GetSalesReturns(string? search, SalesReturnStatus? status, int page, int pageSize)
    {
        var query = _context.SalesReturns
            .AsSplitQuery()
            .Include(r => r.SalesOrder)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.ReturnNumber.Contains(search) || r.SalesOrder.OrderNumber.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<SalesReturnDto>(_mapper.Map<List<SalesReturnDto>>(items), total, page, pageSize);
    }

    public async Task<SalesReturnDto> GetSalesReturnById(Guid id)
    {
        var ret = await _context.SalesReturns
            .AsSplitQuery()
            .Include(r => r.SalesOrder)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new EntityNotFoundException("SalesReturn", id);
        return _mapper.Map<SalesReturnDto>(ret);
    }

    public async Task<Guid> CreateSalesReturn(SalesReturnDto dto)
    {
        _ = await _context.SalesOrders.FindAsync(dto.SalesOrderId)
            ?? throw new EntityNotFoundException("SalesOrder", dto.SalesOrderId);

        var ret = new SalesReturn
        {
            ReturnNumber = NumberGenerator.GenerateOrderNumber("SR"),
            SalesOrderId = dto.SalesOrderId,
            Reason = dto.Reason,
            Status = SalesReturnStatus.Draft
        };

        foreach (var item in dto.ItemsLine.Where(i => i.IsSelected))
        {
            ret.Items.Add(new SalesReturnItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice
            });
        }

        ret.TotalAmount = ret.Items.Sum(i => i.LineTotal);
        _context.SalesReturns.Add(ret);
        await _context.SaveChangesAsync();
        return ret.Id;
    }

    public async Task ApproveSalesReturn(Guid id)
    {
        var ret = await _context.SalesReturns
            .Include(r => r.Items)
            .Include(r => r.SalesOrder)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new EntityNotFoundException("SalesReturn", id);

        ret.Status = SalesReturnStatus.Approved;

        if (ret.SalesOrder.WarehouseId.HasValue)
        {
            foreach (var item in ret.Items)
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == ret.SalesOrder.WarehouseId.Value);
                if (inventory != null)
                {
                    inventory.Quantity += item.Quantity;
                    _context.StockMovements.Add(new StockMovement
                    {
                        ProductId = item.ProductId,
                        WarehouseId = inventory.WarehouseId,
                        Type = StockMovementType.Return,
                        Quantity = item.Quantity,
                        MovementDate = DateTime.UtcNow,
                        Reference = ret.ReturnNumber
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task CompleteSalesReturn(Guid id)
    {
        var ret = await _context.SalesReturns.FindAsync(id)
            ?? throw new EntityNotFoundException("SalesReturn", id);
        ret.Status = SalesReturnStatus.Completed;
        await _context.SaveChangesAsync();
    }
}
