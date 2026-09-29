using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace TradeFlow.Infrastructure.Services;

public class PurchaseReturnService : IPurchaseReturnService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public PurchaseReturnService(ApplicationDbContext context, IMapper mapper, IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _context = context;
        _mapper = mapper;
        _contextFactory = contextFactory;
    }

    public async Task<PagedResult<PurchaseReturnDto>> GetPurchaseReturns(string? search, PurchaseReturnStatus? status, int page, int pageSize)
    {
        var query = _context.PurchaseReturns
            .AsSplitQuery()
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

    /// <summary>
    /// Server-side paged purchase-return list used by the Purchase Returns grid. Mirrors
    /// OrderService.GetSalesOrdersAsync: zero-based paging, filterable, sortable,
    /// and returns per-status counts in <see cref="PagedResultNew{T}.Stats"/>.
    /// </summary>
    public async Task<PagedResultNew<PurchaseReturnDto>> GetPurchaseReturnsAsync(PurchaseReturnPagedRequest request)
    {
        // Own context for the whole read: the list can be re-entered while another query on the
        // scoped context is still in flight, and a DbContext cannot run two commands at once.
        await using var read = await _contextFactory.CreateDbContextAsync();
        var response = new PagedResultNew<PurchaseReturnDto>();

        var page = Math.Max(0, request.CurrentPage);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = request.SearchTerm?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        IQueryable<PurchaseReturn> query;
        if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
            query = read.PurchaseReturns.IgnoreQueryFilters().AsNoTracking()
                .Where(r => !r.IsDeleted && r.TenantId == request.TenantId.Value);
        else
            query = read.PurchaseReturns.AsNoTracking().Where(r => !r.IsDeleted);

        // Stats are computed over the non-status filters so the tiles keep showing
        // every bucket while a single status tile is active.
        var statsSource = ApplyPurchaseReturnFilters(query, request, search, hasSearch);

        var groups = await statsSource
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byStatus = groups.ToDictionary(g => g.Status, g => g.Count);
        response.Stats["TotalCount"] = byStatus.Values.Sum();
        foreach (var s in Enum.GetValues<PurchaseReturnStatus>())
            response.Stats[$"{s}Count"] = byStatus.TryGetValue(s, out var c) ? c : 0;
        response.Stats["TotalReturned"] = (int)Math.Round(
            await statsSource.SumAsync(r => (decimal?)r.TotalAmount) ?? 0m);

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        query = ApplyPurchaseReturnFilters(query, request, search, hasSearch);

        var sortAsc = request.SortAscending;
        query = (request.SortColumn ?? "CreatedAt") switch
        {
            "ReturnNumber" => sortAsc ? query.OrderBy(r => r.ReturnNumber) : query.OrderByDescending(r => r.ReturnNumber),
            "PurchaseOrderNumber" => sortAsc
                ? query.OrderBy(r => r.PurchaseOrder.OrderNumber)
                : query.OrderByDescending(r => r.PurchaseOrder.OrderNumber),
            "TotalAmount" => sortAsc ? query.OrderBy(r => r.TotalAmount) : query.OrderByDescending(r => r.TotalAmount),
            "Reason" => sortAsc ? query.OrderBy(r => r.Reason) : query.OrderByDescending(r => r.Reason),
            "Status" => sortAsc ? query.OrderBy(r => r.Status) : query.OrderByDescending(r => r.Status),
            _ => sortAsc ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt),
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .AsSplitQuery()
            .Include(r => r.PurchaseOrder)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        response.Items = _mapper.Map<List<PurchaseReturnDto>>(items);
        response.TotalCount = totalCount;
        response.CurrentPage = page;
        response.PageSize = pageSize;

        return response;
    }

    private static IQueryable<PurchaseReturn> ApplyPurchaseReturnFilters(
        IQueryable<PurchaseReturn> query, PurchaseReturnPagedRequest request, string? search, bool hasSearch)
    {
        if (hasSearch)
        {
            query = query.Where(r =>
                EF.Functions.Like(r.ReturnNumber, $"%{search}%") ||
                EF.Functions.Like(r.PurchaseOrder.OrderNumber, $"%{search}%") ||
                EF.Functions.Like(r.Reason, $"%{search}%"));
        }

        if (request.PurchaseOrderId.HasValue && request.PurchaseOrderId != Guid.Empty)
            query = query.Where(r => r.PurchaseOrderId == request.PurchaseOrderId.Value);

        if (request.From.HasValue)
            query = query.Where(r => r.CreatedAt >= request.From.Value.Date);

        if (request.To.HasValue)
        {
            var toExclusive = request.To.Value.Date.AddDays(1);
            query = query.Where(r => r.CreatedAt < toExclusive);
        }

        return query;
    }

    public async Task<PurchaseReturnDto> GetPurchaseReturnById(Guid id)
    {
        var ret = await _context.PurchaseReturns
            .AsSplitQuery()
            .Include(r => r.PurchaseOrder)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new EntityNotFoundException("PurchaseReturn", id);
        return _mapper.Map<PurchaseReturnDto>(ret);
    }

    public async Task<Guid> CreatePurchaseReturn(PurchaseReturnDto dto)
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

        var items= dto.ItemsLine.Where(i => i.IsSelected).ToList();

        foreach (var item in items)
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
