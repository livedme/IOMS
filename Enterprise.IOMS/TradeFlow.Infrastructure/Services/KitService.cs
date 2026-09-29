using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TradeFlow.Infrastructure.Services;

public class KitService : IKitService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public KitService(ApplicationDbContext context, IMapper mapper, IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _context = context;
        _mapper = mapper;
        _contextFactory = contextFactory;
    }

    public async Task<PagedResultNew<KitDto>> GetKitsPagedAsync(KitPagedRequest request)
    {
        // Own context for the whole read: the list can be re-entered while another query on the
        // scoped context is still in flight, and a DbContext cannot run two commands at once.
        await using var read = await _contextFactory.CreateDbContextAsync();
        var response = new PagedResultNew<KitDto>();

        var page = Math.Max(0, request.CurrentPage);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = request.SearchTerm?.Trim();

        IQueryable<Kit> query = read.Kits
            .AsNoTracking()
            .Include(k => k.Product)
            .Include(k => k.Components).ThenInclude(c => c.ComponentProduct);

        if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
            query = query.Where(k => k.TenantId == request.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(k =>
                EF.Functions.Like(k.Product.Name, $"%{search}%") ||
                EF.Functions.Like(k.Product.SKU, $"%{search}%"));
        }

        var statRows = await query
            .GroupBy(k => new { k.IsActive, ComponentCount = k.Components.Count })
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        response.Stats["TotalCount"] = statRows.Sum(r => r.Count);
        response.Stats["ActiveCount"] = statRows.Where(r => r.Key.IsActive).Sum(r => r.Count);
        response.Stats["InactiveCount"] = statRows.Where(r => !r.Key.IsActive).Sum(r => r.Count);
        response.Stats["TotalComponents"] = statRows.Sum(r => r.Key.ComponentCount * r.Count);
        response.Stats["SingleComponentCount"] = statRows.Where(r => r.Key.ComponentCount == 1).Sum(r => r.Count);
        response.Stats["AverageComponents"] = response.Stats["TotalCount"] == 0
            ? 0
            : (int)Math.Round(
                statRows.Sum(r => (double)r.Key.ComponentCount * r.Count) / response.Stats["TotalCount"]);

        if (request.IsActive.HasValue)
            query = query.Where(k => k.IsActive == request.IsActive.Value);

        var sortAsc = request.SortAscending;
        query = (request.SortColumn ?? "ProductName") switch
        {
            "ProductSKU" => sortAsc ? query.OrderBy(k => k.Product.SKU) : query.OrderByDescending(k => k.Product.SKU),
            "ComponentCount" => sortAsc
                ? query.OrderBy(k => k.Components.Count).ThenBy(k => k.Product.Name)
                : query.OrderByDescending(k => k.Components.Count).ThenBy(k => k.Product.Name),
            "IsActive" => sortAsc ? query.OrderBy(k => k.IsActive) : query.OrderByDescending(k => k.IsActive),
            _ => sortAsc ? query.OrderBy(k => k.Product.Name) : query.OrderByDescending(k => k.Product.Name),
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip(page * pageSize).Take(pageSize).ToListAsync();

        response.Items = _mapper.Map<List<KitDto>>(items);
        response.TotalCount = totalCount;
        response.CurrentPage = page;
        response.PageSize = pageSize;

        return response;
    }

    public async Task<List<KitDto>> GetKits()
    {
        var kits = await _context.Kits
            .Include(k => k.Product)
            .Include(k => k.Components).ThenInclude(c => c.ComponentProduct)
            .ToListAsync();
        return _mapper.Map<List<KitDto>>(kits);
    }

    public async Task<KitDto> GetKitById(Guid id)
    {
        var kit = await _context.Kits
            .Include(k => k.Product)
            .Include(k => k.Components).ThenInclude(c => c.ComponentProduct)
            .FirstOrDefaultAsync(k => k.Id == id)
            ?? throw new EntityNotFoundException("Kit", id);
        return _mapper.Map<KitDto>(kit);
    }

    public async Task<Guid> CreateKit(CreateKitDto dto)
    {
        var kit = new Kit
        {
            ProductId = dto.ProductId,
            IsActive = true
        };

        foreach (var c in dto.Components)
        {
            kit.Components.Add(new KitComponent
            {
                ComponentProductId = c.ComponentProductId,
                Quantity = c.Quantity
            });
        }

        _context.Kits.Add(kit);
        await _context.SaveChangesAsync();
        return kit.Id;
    }

    public async Task AssembleKit(KitAssemblyDto dto)
    {
        var kit = await _context.Kits
            .Include(k => k.Components)
            .FirstOrDefaultAsync(k => k.Id == dto.KitId)
            ?? throw new EntityNotFoundException("Kit", dto.KitId);

        foreach (var comp in kit.Components)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == comp.ComponentProductId && i.WarehouseId == dto.WarehouseId)
                ?? throw new DomainException($"No inventory for component {comp.ComponentProductId} in warehouse.");

            var required = comp.Quantity * dto.Quantity;
            if (inv.Quantity < required)
                throw new DomainException($"Insufficient stock for component. Need {required}, have {inv.Quantity}.");

            inv.Quantity -= required;
            _context.StockMovements.Add(new StockMovement
            {
                ProductId = comp.ComponentProductId,
                WarehouseId = dto.WarehouseId,
                Type = StockMovementType.KitAssembly,
                Quantity = -required,
                MovementDate = DateTime.UtcNow,
                Reference = $"Kit-{kit.Id.ToString()[..8]}"
            });
        }

        var kitInv = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == kit.ProductId && i.WarehouseId == dto.WarehouseId);

        if (kitInv == null)
        {
            kitInv = new Inventory { ProductId = kit.ProductId, WarehouseId = dto.WarehouseId, Quantity = 0 };
            _context.Inventories.Add(kitInv);
        }

        kitInv.Quantity += dto.Quantity;
        _context.StockMovements.Add(new StockMovement
        {
            ProductId = kit.ProductId,
            WarehouseId = dto.WarehouseId,
            Type = StockMovementType.KitAssembly,
            Quantity = dto.Quantity,
            MovementDate = DateTime.UtcNow,
            Reference = $"Kit-{kit.Id.ToString()[..8]}"
        });

        await _context.SaveChangesAsync();
    }

    public async Task DisassembleKit(KitAssemblyDto dto)
    {
        var kit = await _context.Kits
            .Include(k => k.Components)
            .FirstOrDefaultAsync(k => k.Id == dto.KitId)
            ?? throw new EntityNotFoundException("Kit", dto.KitId);

        var kitInv = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == kit.ProductId && i.WarehouseId == dto.WarehouseId)
            ?? throw new DomainException("No kit inventory to disassemble.");

        if (kitInv.Quantity < dto.Quantity)
            throw new DomainException($"Insufficient kit stock. Have {kitInv.Quantity}, need {dto.Quantity}.");

        kitInv.Quantity -= dto.Quantity;
        _context.StockMovements.Add(new StockMovement
        {
            ProductId = kit.ProductId,
            WarehouseId = dto.WarehouseId,
            Type = StockMovementType.KitDisassembly,
            Quantity = -dto.Quantity,
            MovementDate = DateTime.UtcNow,
            Reference = $"Kit-{kit.Id.ToString()[..8]}"
        });

        foreach (var comp in kit.Components)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == comp.ComponentProductId && i.WarehouseId == dto.WarehouseId);

            if (inv == null)
            {
                inv = new Inventory { ProductId = comp.ComponentProductId, WarehouseId = dto.WarehouseId, Quantity = 0 };
                _context.Inventories.Add(inv);
            }

            var returned = comp.Quantity * dto.Quantity;
            inv.Quantity += returned;
            _context.StockMovements.Add(new StockMovement
            {
                ProductId = comp.ComponentProductId,
                WarehouseId = dto.WarehouseId,
                Type = StockMovementType.KitDisassembly,
                Quantity = returned,
                MovementDate = DateTime.UtcNow,
                Reference = $"Kit-{kit.Id.ToString()[..8]}"
            });
        }

        await _context.SaveChangesAsync();
    }
}
