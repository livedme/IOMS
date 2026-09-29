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

public class SalesReturnService : ISalesReturnService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public SalesReturnService(ApplicationDbContext context, IMapper mapper, IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _context = context;
        _mapper = mapper;
        _contextFactory = contextFactory;
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

    /// <summary>
    /// Server-side paged sales-return list used by the Sales Returns grid. Mirrors
    /// OrderService.GetSalesOrdersAsync: zero-based paging, filterable, sortable,
    /// and returns per-status counts in <see cref="PagedResultNew{T}.Stats"/>.
    /// </summary>
    public async Task<PagedResultNew<SalesReturnDto>> GetSalesReturnsAsync(SalesReturnPagedRequest request)
    {
        // Own context for the whole read: the list can be re-entered while another query on the
        // scoped context is still in flight, and a DbContext cannot run two commands at once.
        await using var read = await _contextFactory.CreateDbContextAsync();
        var response = new PagedResultNew<SalesReturnDto>();

        var page = Math.Max(0, request.CurrentPage);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = request.SearchTerm?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        IQueryable<SalesReturn> query;
        if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
            query = read.SalesReturns.IgnoreQueryFilters().AsNoTracking()
                .Where(r => !r.IsDeleted && r.TenantId == request.TenantId.Value);
        else
            query = read.SalesReturns.AsNoTracking().Where(r => !r.IsDeleted);

        // Stats are computed over the non-status filters so the tiles keep showing
        // every bucket while a single status tile is active.
        var statsSource = ApplySalesReturnFilters(query, request, search, hasSearch);

        var groups = await statsSource
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byStatus = groups.ToDictionary(g => g.Status, g => g.Count);
        response.Stats["TotalCount"] = byStatus.Values.Sum();
        foreach (var s in Enum.GetValues<SalesReturnStatus>())
            response.Stats[$"{s}Count"] = byStatus.TryGetValue(s, out var c) ? c : 0;
        response.Stats["TotalReturned"] = (int)Math.Round(
            await statsSource.SumAsync(r => (decimal?)r.TotalAmount) ?? 0m);

        if (request.Status.HasValue)
        {
            var status = request.Status.Value;
            query = query.Where(r => r.Status == status);
        }

        query = ApplySalesReturnFilters(query, request, search, hasSearch);

        var sortAsc = request.SortAscending;
        query = (request.SortColumn ?? "CreatedAt") switch
        {
            "ReturnNumber" => sortAsc ? query.OrderBy(r => r.ReturnNumber) : query.OrderByDescending(r => r.ReturnNumber),
            "SalesOrderNumber" => sortAsc
                ? query.OrderBy(r => r.SalesOrder.OrderNumber)
                : query.OrderByDescending(r => r.SalesOrder.OrderNumber),
            "TotalAmount" => sortAsc ? query.OrderBy(r => r.TotalAmount) : query.OrderByDescending(r => r.TotalAmount),
            "Reason" => sortAsc ? query.OrderBy(r => r.Reason) : query.OrderByDescending(r => r.Reason),
            "Status" => sortAsc ? query.OrderBy(r => r.Status) : query.OrderByDescending(r => r.Status),
            _ => sortAsc ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt),
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .AsSplitQuery()
            .Include(r => r.SalesOrder)
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        response.Items = _mapper.Map<List<SalesReturnDto>>(items);
        response.TotalCount = totalCount;
        response.CurrentPage = page;
        response.PageSize = pageSize;

        return response;
    }

    private static IQueryable<SalesReturn> ApplySalesReturnFilters(
        IQueryable<SalesReturn> query, SalesReturnPagedRequest request, string? search, bool hasSearch)
    {
        if (hasSearch)
        {
            query = query.Where(r =>
                EF.Functions.Like(r.ReturnNumber, $"%{search}%") ||
                EF.Functions.Like(r.SalesOrder.OrderNumber, $"%{search}%") ||
                EF.Functions.Like(r.Reason, $"%{search}%"));
        }

        if (request.SalesOrderId.HasValue && request.SalesOrderId != Guid.Empty)
            query = query.Where(r => r.SalesOrderId == request.SalesOrderId.Value);

        if (request.From.HasValue)
            query = query.Where(r => r.CreatedAt >= request.From.Value.Date);

        if (request.To.HasValue)
        {
            var toExclusive = request.To.Value.Date.AddDays(1);
            query = query.Where(r => r.CreatedAt < toExclusive);
        }

        return query;
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
