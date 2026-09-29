using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TradeFlow.Infrastructure.Services;

public class StocktakeService : IStocktakeService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public StocktakeService(ApplicationDbContext context, IMapper mapper, IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _context = context;
        _mapper = mapper;
        _contextFactory = contextFactory;
    }

    public async Task<PagedResultNew<StocktakeDto>> GetStocktakesPagedAsync(StocktakePagedRequest request)
    {
        // Own context for the whole read: the list can be re-entered while another query on the
        // scoped context is still in flight, and a DbContext cannot run two commands at once.
        await using var read = await _contextFactory.CreateDbContextAsync();
        var response = new PagedResultNew<StocktakeDto>();

        var page = Math.Max(0, request.CurrentPage);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = request.SearchTerm?.Trim();

        IQueryable<Stocktake> query = read.Stocktakes
            .AsSplitQuery()
            .AsNoTracking()
            .Include(s => s.Warehouse)
            .Include(s => s.Items);

        if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
            query = query.Where(s => s.TenantId == request.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                EF.Functions.Like(s.Warehouse.Name, $"%{search}%") ||
                EF.Functions.Like(s.Notes, $"%{search}%"));

        // Stats are computed over the non-status filters so the tiles keep showing
        // every bucket while a single status tile is active.
        var groups = await query
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byStatus = groups.ToDictionary(g => g.Status, g => g.Count);
        response.Stats["TotalCount"] = byStatus.Values.Sum();
        foreach (var s in Enum.GetValues<StocktakeStatus>())
            response.Stats[$"{s}Count"] = byStatus.TryGetValue(s, out var c) ? c : 0;

        if (request.Status.HasValue)
        {
            var status = request.Status.Value;
            query = query.Where(s => s.Status == status);
        }

        if (request.WarehouseId.HasValue && request.WarehouseId != Guid.Empty)
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);

        if (request.From.HasValue)
            query = query.Where(s => s.StartDate >= request.From.Value.Date);

        if (request.To.HasValue)
        {
            var toExclusive = request.To.Value.Date.AddDays(1);
            query = query.Where(s => s.StartDate < toExclusive);
        }

        var sortAsc = request.SortAscending;
        query = (request.SortColumn ?? "StartDate") switch
        {
            "Warehouse" => sortAsc ? query.OrderBy(s => s.Warehouse.Name) : query.OrderByDescending(s => s.Warehouse.Name),
            "EndDate" => sortAsc ? query.OrderBy(s => s.EndDate) : query.OrderByDescending(s => s.EndDate),
            "Status" => sortAsc ? query.OrderBy(s => s.Status) : query.OrderByDescending(s => s.Status),
            _ => sortAsc ? query.OrderBy(s => s.StartDate) : query.OrderByDescending(s => s.StartDate),
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip(page * pageSize).Take(pageSize).ToListAsync();

        response.Items = _mapper.Map<List<StocktakeDto>>(items);
        response.TotalCount = totalCount;
        response.CurrentPage = page;
        response.PageSize = pageSize;

        return response;
    }

    public async Task<PagedResult<StocktakeDto>> GetStocktakes(StocktakeStatus? status, int page, int pageSize)
    {
        var query = _context.Stocktakes
            .Include(s => s.Warehouse)
            .Include(s => s.Items)
            .AsQueryable();

        if (status.HasValue) query = query.Where(s => s.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(s => s.StartDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<StocktakeDto>(_mapper.Map<List<StocktakeDto>>(items), total, page, pageSize);
    }

    public async Task<StocktakeDto> GetStocktakeById(Guid id)
    {
        var st = await _context.Stocktakes
            .AsSplitQuery()
            .Include(s => s.Warehouse)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new EntityNotFoundException("Stocktake", id);
        return _mapper.Map<StocktakeDto>(st);
    }

    public async Task<StocktakeVarianceDto> GetVarianceReport(Guid stocktakeId)
    {
        var st = await _context.Stocktakes
            .AsSplitQuery()
            .Include(s => s.Warehouse)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == stocktakeId)
            ?? throw new EntityNotFoundException("Stocktake", stocktakeId);

        var itemDtos = _mapper.Map<List<StocktakeItemDto>>(st.Items);
        var totalVariance = st.Items.Sum(i => i.Variance);
        var valueVariance = st.Items.Sum(i => i.Variance * i.Product.CostPrice);

        return new StocktakeVarianceDto(st.Id, st.Warehouse.Name, itemDtos, totalVariance, valueVariance);
    }

    public async Task<Guid> CreateStocktake(CreateStocktakeDto dto)
    {
        var inventories = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.WarehouseId == dto.WarehouseId && i.Quantity > 0)
            .ToListAsync();

        var stocktake = new Stocktake
        {
            WarehouseId = dto.WarehouseId,
            StartDate = DateTime.UtcNow,
            Notes = dto.Notes,
            Status = StocktakeStatus.Draft
        };

        foreach (var inv in inventories)
        {
            stocktake.Items.Add(new StocktakeItem
            {
                ProductId = inv.ProductId,
                SystemQuantity = inv.Quantity
            });
        }

        _context.Stocktakes.Add(stocktake);
        await _context.SaveChangesAsync();
        return stocktake.Id;
    }

    public async Task RecordCount(RecordStocktakeCountDto dto)
    {
        var item = await _context.StocktakeItems
            .Include(i => i.Stocktake)
            .FirstOrDefaultAsync(i => i.Id == dto.StocktakeItemId)
            ?? throw new EntityNotFoundException("StocktakeItem", dto.StocktakeItemId);

        item.CountedQuantity = dto.CountedQuantity;
        item.Notes = dto.Notes;

        if (item.Stocktake.Status == StocktakeStatus.Draft)
            item.Stocktake.Status = StocktakeStatus.InProgress;

        await _context.SaveChangesAsync();
    }

    public async Task ApproveStocktake(Guid stocktakeId)
    {
        var st = await _context.Stocktakes
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == stocktakeId)
            ?? throw new EntityNotFoundException("Stocktake", stocktakeId);

        st.Status = StocktakeStatus.Approved;
        st.EndDate = DateTime.UtcNow;

        foreach (var item in st.Items.Where(i => i.CountedQuantity.HasValue && i.Variance != 0))
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == st.WarehouseId);
            if (inv != null)
            {
                inv.Quantity = item.CountedQuantity!.Value;
                _context.StockMovements.Add(new StockMovement
                {
                    ProductId = item.ProductId,
                    WarehouseId = st.WarehouseId,
                    Type = StockMovementType.Adjustment,
                    Quantity = item.Variance,
                    MovementDate = DateTime.UtcNow,
                    Reference = $"Stocktake-{st.Id.ToString()[..8]}",
                    Notes = $"Stocktake variance adjustment"
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task CancelStocktake(Guid stocktakeId)
    {
        var st = await _context.Stocktakes.FindAsync(stocktakeId)
            ?? throw new EntityNotFoundException("Stocktake", stocktakeId);
        st.Status = StocktakeStatus.Cancelled;
        await _context.SaveChangesAsync();
    }
}
