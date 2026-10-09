using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;

namespace TradeFlow.Infrastructure.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public InventoryService(ApplicationDbContext context, IMapper mapper, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _context = context;
            _mapper = mapper;
            _contextFactory = contextFactory;
        }

        public async Task<PagedResultNew<StockMovementDto>> GetStockMovementsPagedAsync(StockMovementPagedRequest request)
        {
            // Own context for the whole read: the list can be re-entered while another query on the
            // scoped context is still in flight, and a DbContext cannot run two commands at once.
            await using var read = await _contextFactory.CreateDbContextAsync();
            var response = new PagedResultNew<StockMovementDto>();

            var page = Math.Max(0, request.CurrentPage);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var search = request.SearchTerm?.Trim();
            var hasSearch = !string.IsNullOrWhiteSpace(search);

            IQueryable<StockMovement> query;
            if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
                query = read.StockMovements.IgnoreQueryFilters().AsNoTracking()
                    .Where(m => !m.IsDeleted && m.TenantId == request.TenantId.Value);
            else
                query = read.StockMovements.AsNoTracking().Where(m => !m.IsDeleted);

            // The Stock Transfers grid is this same query with Type pinned to Transfer.
            if (request.Type.HasValue)
            {
                var type = request.Type.Value;
                query = query.Where(m => m.Type == type);
            }

            // Stats ignore the type filter so the tiles keep showing every bucket.
            var statsSource = ApplyMovementFilters(query, request, search, hasSearch);
            var groups = await statsSource
                .GroupBy(m => m.Type)
                .Select(g => new { Type = g.Key, Count = g.Count(), Net = g.Sum(m => m.Quantity) })
                .ToListAsync();

            var byType = groups.ToDictionary(g => g.Type, g => g);
            response.Stats["TotalCount"] = byType.Values.Sum(g => g.Count);
            foreach (var t in Enum.GetValues<StockMovementType>())
            {
                response.Stats[$"{t}Count"] = byType.TryGetValue(t, out var c) ? c.Count : 0;
                response.Stats[$"{t}Units"] = byType.TryGetValue(t, out var u) ? (int)Math.Abs((decimal)u.Net) : 0;
            }
            response.Stats["NetQuantity"] = (int)groups.Sum(g => g.Net);
            response.Stats["StockInCount"] = byType.TryGetValue(StockMovementType.In, out var inRow) ? inRow.Count : 0;
            response.Stats["StockOutCount"] = byType.TryGetValue(StockMovementType.Out, out var outRow) ? outRow.Count : 0;
            response.Stats["DistinctProductCount"] = await statsSource.Select(m => m.ProductId).Distinct().CountAsync();
            response.Stats["WarehouseCount"] = await statsSource.Select(m => m.WarehouseId).Distinct().CountAsync();

            query = ApplyMovementFilters(query, request, search, hasSearch);

            // Direction tab: applied after stats so the tiles keep showing both legs.
            if (request.IsInbound.HasValue)
                query = query.Where(m => request.IsInbound.Value ? m.Quantity > 0 : m.Quantity < 0);

            var sortAsc = request.SortAscending;
            query = (request.SortColumn ?? "MovementDate") switch
            {
                "ProductName" => sortAsc ? query.OrderBy(m => m.Product.Name) : query.OrderByDescending(m => m.Product.Name),
                "WarehouseName" => sortAsc ? query.OrderBy(m => m.Warehouse.Name) : query.OrderByDescending(m => m.Warehouse.Name),
                "Type" => sortAsc ? query.OrderBy(m => m.Type) : query.OrderByDescending(m => m.Type),
                "Quantity" => sortAsc ? query.OrderBy(m => m.Quantity) : query.OrderByDescending(m => m.Quantity),
                "Reference" => sortAsc ? query.OrderBy(m => m.Reference) : query.OrderByDescending(m => m.Reference),
                _ => sortAsc ? query.OrderBy(m => m.MovementDate) : query.OrderByDescending(m => m.MovementDate),
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .AsSplitQuery()
                .Include(m => m.Product)
                .Include(m => m.Warehouse)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = items
                .Select(m => new StockMovementDto(
                    m.Id,
                    m.MovementDate,
                    m.ProductId,
                    m.Product?.Name ?? "",
                    m.WarehouseId,
                    m.Warehouse?.Name ?? "",
                    m.Type,
                    m.Quantity,
                    m.Reference,
                    m.Notes,
                    m.SourceWarehouseId,
                    m.DestinationWarehouseId))
                .ToList();
            response.TotalCount = totalCount;
            response.CurrentPage = page;
            response.PageSize = pageSize;

            return response;
        }

        private static IQueryable<StockMovement> ApplyMovementFilters(
            IQueryable<StockMovement> query, StockMovementPagedRequest request, string? search, bool hasSearch)
        {
            if (hasSearch)
            {
                query = query.Where(m =>
                    EF.Functions.Like(m.Product.Name, $"%{search}%") ||
                    EF.Functions.Like(m.Product.SKU, $"%{search}%") ||
                    EF.Functions.Like(m.Reference, $"%{search}%") ||
                    EF.Functions.Like(m.Notes, $"%{search}%"));
            }

            if (request.WarehouseId.HasValue && request.WarehouseId != Guid.Empty)
                query = query.Where(m =>
                    m.WarehouseId == request.WarehouseId.Value ||
                    m.SourceWarehouseId == request.WarehouseId.Value ||
                    m.DestinationWarehouseId == request.WarehouseId.Value);

            if (request.From.HasValue)
                query = query.Where(m => m.MovementDate >= request.From.Value.Date);

            if (request.To.HasValue)
            {
                var toExclusive = request.To.Value.Date.AddDays(1);
                query = query.Where(m => m.MovementDate < toExclusive);
            }

            return query;
        }

        public async Task<PagedResultNew<StockLevelDto>> GetStockLevelsPagedAsync(StockLevelPagedRequest request)
        {
            // Own context for the whole read: the list can be re-entered while another query on the
            // scoped context is still in flight, and a DbContext cannot run two commands at once.
            await using var read = await _contextFactory.CreateDbContextAsync();
            var response = new PagedResultNew<StockLevelDto>();

            var page = Math.Max(0, request.CurrentPage);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var search = request.SearchTerm?.Trim();
            var hasSearch = !string.IsNullOrWhiteSpace(search);

            IQueryable<Inventory> query;
            if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
                query = read.Inventories.IgnoreQueryFilters().AsNoTracking()
                    .Where(i => !i.IsDeleted && i.TenantId == request.TenantId.Value);
            else
                query = read.Inventories.AsNoTracking().Where(i => !i.IsDeleted);

            // The Low Stock Alerts view is the same rows pinned to at-or-below reorder level.
            if (request.LowStockOnly)
                query = query.Where(i => i.Quantity <= i.Product.ReorderStockLevel);

            // Stats ignore the status filter so the tiles keep showing every bucket.
            var statsSource = ApplyStockLevelFilters(query, request, search, hasSearch);
            var groups = await statsSource
                .GroupBy(i => new
                {
                    InStock = i.Quantity > 0,
                    OutOfStock = i.Quantity <= 0,
                    HasReserved = i.ReservedQuantity > 0,
                    Low = i.Quantity <= i.Product.ReorderStockLevel
                })
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            response.Stats["TotalCount"] = groups.Sum(g => g.Count);
            response.Stats["InStockCount"] = groups.Where(g => g.Key.InStock).Sum(g => g.Count);
            response.Stats["OutOfStockCount"] = groups.Where(g => g.Key.OutOfStock).Sum(g => g.Count);
            response.Stats["ReservedCount"] = groups.Where(g => g.Key.HasReserved).Sum(g => g.Count);
            response.Stats["LowStockCount"] = groups.Where(g => g.Key.Low).Sum(g => g.Count);
            response.Stats["TotalQuantity"] = (int)Math.Round(
                await statsSource.SumAsync(i => (decimal?)i.Quantity) ?? 0m);
            response.Stats["WarehouseCount"] = await statsSource.Select(i => i.WarehouseId).Distinct().CountAsync();

            query = ApplyStockLevelFilters(query, request, search, hasSearch);

            if (request.Status switch
            {
                "InStock" => true,
                _ => false
            })
            {
                query = query.Where(i => i.Quantity > 0);
            }
            else if (request.Status == "OutOfStock")
            {
                query = query.Where(i => i.Quantity <= 0);
            }
            else if (request.Status == "Reserved")
            {
                query = query.Where(i => i.ReservedQuantity > 0);
            }

            var sortAsc = request.SortAscending;
            query = (request.SortColumn ?? "ProductName") switch
            {
                "ProductSKU" => sortAsc ? query.OrderBy(i => i.Product.SKU) : query.OrderByDescending(i => i.Product.SKU),
                "WarehouseName" => sortAsc ? query.OrderBy(i => i.Warehouse.Name) : query.OrderByDescending(i => i.Warehouse.Name),
                "Quantity" => sortAsc ? query.OrderBy(i => i.Quantity) : query.OrderByDescending(i => i.Quantity),
                "ReservedQuantity" => sortAsc ? query.OrderBy(i => i.ReservedQuantity) : query.OrderByDescending(i => i.ReservedQuantity),
                "AvailableQuantity" => sortAsc
                    ? query.OrderBy(i => i.Quantity - i.ReservedQuantity)
                    : query.OrderByDescending(i => i.Quantity - i.ReservedQuantity),
                "ReorderStockLevel" => sortAsc
                    ? query.OrderBy(i => i.Product.ReorderStockLevel)
                    : query.OrderByDescending(i => i.Product.ReorderStockLevel),
                _ => sortAsc ? query.OrderBy(i => i.Product.Name) : query.OrderByDescending(i => i.Product.Name),
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .AsSplitQuery()
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = items
                .Select(i => new StockLevelDto(
                    i.Id,
                    i.ProductId,
                    i.Product?.Name ?? "",
                    i.Product?.SKU ?? "",
                    i.WarehouseId,
                    i.Warehouse?.Name ?? "",
                    i.Quantity,
                    i.ReservedQuantity,
                    i.Quantity - i.ReservedQuantity,
                    i.Product?.ReorderStockLevel ?? 0,
                    i.BinLocation))
                .ToList();
            response.TotalCount = totalCount;
            response.CurrentPage = page;
            response.PageSize = pageSize;

            return response;
        }

        private static IQueryable<Inventory> ApplyStockLevelFilters(
            IQueryable<Inventory> query, StockLevelPagedRequest request, string? search, bool hasSearch)
        {
            if (hasSearch)
            {
                query = query.Where(i =>
                    EF.Functions.Like(i.Product.Name, $"%{search}%") ||
                    EF.Functions.Like(i.Product.SKU, $"%{search}%") ||
                    EF.Functions.Like(i.BinLocation, $"%{search}%"));
            }

            if (request.WarehouseId.HasValue && request.WarehouseId != Guid.Empty)
                query = query.Where(i => i.WarehouseId == request.WarehouseId.Value);

            return query;
        }

        public async Task<PagedResult<InventoryTrackingDto>> GetInventoryTrackingByWarehouse(Guid warehouseId, int page, int pageSize)
        {
            var query = _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.WarehouseId == warehouseId);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(i => i.Product.Name)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(i => new InventoryTrackingDto(
                i.Id,
                i.ProductId,
                i.Product.Name,
                i.Product.SKU,
                i.WarehouseId,
                i.Warehouse.Name,
                i.Quantity,
                i.ReservedQuantity,
                i.AvailableQuantity,
                i.BinLocation,
                i.SerialNumber,
                i.BatchNumber,
                i.ExpiryDate,
                i.Condition
            )).ToList();

            return new PagedResult<InventoryTrackingDto>(dtos, total, page, pageSize);
        }
        public async Task<PagedResult<InventoryTrackingDto>> GetInventoryTrackingByWarehouses(IEnumerable<Guid> warehouseIds, int page, int pageSize)
        {
            var query = _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => warehouseIds.Contains(i.WarehouseId));

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(i => i.Product.Name)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(i => new InventoryTrackingDto(
                i.Id,
                i.ProductId,
                i.Product.Name,
                i.Product.SKU,
                i.WarehouseId,
                i.Warehouse.Name,
                i.Quantity,
                i.ReservedQuantity,
                i.AvailableQuantity,
                i.BinLocation,
                i.SerialNumber,
                i.BatchNumber,
                i.ExpiryDate,
                i.Condition
            )).ToList();

            return new PagedResult<InventoryTrackingDto>(dtos, total, page, pageSize);
        }

        public async Task<int> GetStockLevel(Guid productId, Guid warehouseId)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);
            return inv?.Quantity ?? 0;
        }

        public async Task AdjustStock(Guid productId, Guid warehouseId, int quantityChange, string reason)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);

            if (inv == null)
            {
                inv = new Inventory { ProductId = productId, WarehouseId = warehouseId, Quantity = 0 };
                _context.Inventories.Add(inv);
            }

            inv.Quantity += quantityChange;
            inv.LastStockDate = DateTime.UtcNow;

            _context.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Type = StockMovementType.Adjustment,
                Quantity = quantityChange,
                Reference = reason,
                Notes = reason
            });

            await _context.SaveChangesAsync();
        }

        public async Task TransferStock(Guid productId, Guid sourceWarehouseId, Guid targetWarehouseId, int quantity, DateTime? movementDate = null, string? notes = null)
        {
            var source = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == sourceWarehouseId);

            if (source == null || source.AvailableQuantity < quantity)
                throw new InsufficientStockException(productId, quantity, source?.AvailableQuantity ?? 0);

            source.Quantity -= quantity;
            source.LastStockDate = DateTime.UtcNow;

            var target = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == targetWarehouseId);

            if (target == null)
            {
                target = new Inventory { ProductId = productId, WarehouseId = targetWarehouseId, Quantity = 0 };
                _context.Inventories.Add(target);
            }

            target.Quantity += quantity;
            target.LastStockDate = DateTime.UtcNow;

            var reference = $"Transfer: {sourceWarehouseId} → {targetWarehouseId}";
            var date = movementDate ?? DateTime.UtcNow;
            _context.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                WarehouseId = sourceWarehouseId,
                Type = StockMovementType.Transfer,
                Quantity = -quantity,
                Reference = reference,
                Notes = notes,
                MovementDate = date,
                SourceWarehouseId = sourceWarehouseId,
                DestinationWarehouseId = targetWarehouseId
            });
            _context.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                WarehouseId = targetWarehouseId,
                Type = StockMovementType.Transfer,
                Quantity = quantity,
                Reference = reference,
                Notes = notes,
                MovementDate = date,
                SourceWarehouseId = sourceWarehouseId,
                DestinationWarehouseId = targetWarehouseId
            });

            await _context.SaveChangesAsync();
        }

        public async Task<List<LowStockAlertDto>> GetLowStockAlerts()
        {
            return await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.Quantity <= i.Product.ReorderStockLevel && i.Product.ReorderStockLevel > 0)
                .Select(i => new LowStockAlertDto(
                    i.ProductId, i.Product.Name, i.Product.SKU,
                    i.Warehouse.Name, i.Quantity, i.Product.ReorderStockLevel))
                .ToListAsync();
        }

        public async Task<PagedResult<InventoryDto>> GetInventoryByWarehouse(Guid warehouseId, int page, int pageSize)
        {
            var query = _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.WarehouseId == warehouseId);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(i => i.Product.Name)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            return new PagedResult<InventoryDto>(_mapper.Map<List<InventoryDto>>(items), total, page, pageSize);
        }
    }
}
