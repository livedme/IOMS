using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Domain.Exceptions;
using IOMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services;

public class StocktakeService : IStocktakeService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public StocktakeService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
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
            .Include(s => s.Warehouse)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new EntityNotFoundException("Stocktake", id);
        return _mapper.Map<StocktakeDto>(st);
    }

    public async Task<StocktakeVarianceDto> GetVarianceReport(Guid stocktakeId)
    {
        var st = await _context.Stocktakes
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
