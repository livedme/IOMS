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
using TradeFlow.Shared.Helpers;

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
            await CreateStockTransferAsync(new StockTransferDtoModel(){
                //SourceWarehouseId = sourceWarehouseId,
                TargetWarehouseId = targetWarehouseId,
                Notes = notes,
                ItemsLine = new List<StockTransferLineDto> { new(Guid.NewGuid(), productId, quantity) },
                TransferDate = movementDate
            });
        }

        public async Task<string> CreateStockTransferAsync(StockTransferDtoModel dto)
        {
            try
            {
                if (dto.TargetWarehouseId == Guid.Empty)
                    throw new InvalidOperationException("Target warehouse is required.");

                var lines = dto.ItemsLine.Where(i => i.Quantity > 0).ToList();
                if (lines.Count == 0)
                    throw new InvalidOperationException("Transfer must contain at least one item line.");

                var date = dto.TransferDate ?? DateTime.UtcNow;
                var mainItem= lines.OrderByDescending(l => l.Quantity).FirstOrDefault(); // Sort lines by quantity descending)
                var transfer = new StockTransfer
                {
                    TransferNumber = NumberGenerator.GenerateOrderNumber("ST"),
                    TransferDate = date,
                    SourceWarehouseId = mainItem.SourceWarehouseId,
                    TargetWarehouseId = dto.TargetWarehouseId,
                    Status = StockTransferStatus.Completed,
                    Notes = dto.Notes
                };

                foreach (var line in lines)
                {
                    var source = await FindInventoryAsync(line.ProductId, line.SourceWarehouseId);

                    if (source == null || source.AvailableQuantity < line.Quantity)
                        throw new InsufficientStockException(line.ProductId, line.Quantity, source?.AvailableQuantity ?? 0);

                    source.Quantity -= line.Quantity;
                    source.LastStockDate = DateTime.UtcNow;

                    var target = await FindInventoryAsync(line.ProductId, dto.TargetWarehouseId);

                    if (target == null)
                    {
                        target = new Inventory { ProductId = line.ProductId, WarehouseId = dto.TargetWarehouseId, Quantity = 0, ReservedQuantity=0};
                        _context.Inventories.Add(target);
                    }

                    target.Quantity += line.Quantity;
                    target.LastStockDate = DateTime.UtcNow;

                    transfer.Items.Add(new StockTransferItem
                    {
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                        SourceInventory = source,
                        TargetInventory = target,
                        Notes = dto.Notes
                    });

                    _context.StockMovements.Add(new StockMovement
                    {
                        ProductId = line.ProductId,
                        WarehouseId = line.SourceWarehouseId,
                        Type = StockMovementType.Transfer,
                        Quantity = -line.Quantity,
                        Reference = transfer.TransferNumber,
                        Notes = dto.Notes,
                        MovementDate = date,
                        SourceWarehouseId = line.SourceWarehouseId,
                        DestinationWarehouseId = dto.TargetWarehouseId
                    });
                    _context.StockMovements.Add(new StockMovement
                    {
                        ProductId = line.ProductId,
                        WarehouseId = dto.TargetWarehouseId,
                        Type = StockMovementType.Transfer,
                        Quantity = line.Quantity,
                        Reference = transfer.TransferNumber,
                        Notes = dto.Notes,
                        MovementDate = date,
                        SourceWarehouseId = line.SourceWarehouseId,
                        DestinationWarehouseId = dto.TargetWarehouseId
                    });
                }

                _context.StockTransfers.Add(transfer);
                await _context.SaveChangesAsync();
                return transfer.TransferNumber;
            }catch(Exception ex)
            {
                // Log the exception or handle it as needed
                throw new Exception("An error occurred while creating the stock transfer.", ex);
            }
        }

        public async Task<string> UpdateStockTransferAsync(Guid id, StockTransferDtoModel dto)
        {
            var transfer = await _context.StockTransfers
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new EntityNotFoundException("StockTransfer", id);

            if (transfer.Status == StockTransferStatus.Cancelled)
                throw new InvalidOperationException($"Transfer {transfer.TransferNumber} is cancelled and cannot be edited.");

           // if (dto.SourceWarehouseId == dto.TargetWarehouseId)
              //  throw new InvalidOperationException("Source and target warehouses must be different.");

            var lines = dto.ItemsLine.Where(i => i.Quantity > 0).ToList();
            if (lines.Count == 0)
                throw new InvalidOperationException("Transfer must contain at least one item line.");

            var mainItem = lines.OrderByDescending(l => l.Quantity).FirstOrDefault(); // Sort lines by quantity descending)
            // 1. Reverse the previously posted lines against the ORIGINAL warehouses.
            await ReverseTransferLinesAsync(transfer, "edit");

            // 2. Remove the old lines and their movement legs.
            var oldMovements = await _context.StockMovements
                .Where(m => m.Reference == transfer.TransferNumber)
                .ToListAsync();
            _context.StockMovements.RemoveRange(oldMovements);
            _context.StockTransferItems.RemoveRange(transfer.Items);

            // 3. Update the header and post the new lines (same as create).
            var date = dto.TransferDate ?? DateTime.UtcNow;
            transfer.TransferDate = date;
            transfer.SourceWarehouseId = mainItem.SourceWarehouseId;
            transfer.TargetWarehouseId = dto.TargetWarehouseId;
            transfer.Notes = dto.Notes; 
            transfer.Status = StockTransferStatus.Completed;

            foreach (var line in lines)
            {
                var source = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == line.ProductId && i.WarehouseId == line.SourceWarehouseId);

                if (source == null || source.AvailableQuantity < line.Quantity)
                    throw new InsufficientStockException(line.ProductId, line.Quantity, source?.AvailableQuantity ?? 0);

                source.Quantity -= line.Quantity;
                source.LastStockDate = DateTime.UtcNow;

                var target = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == line.ProductId && i.WarehouseId == dto.TargetWarehouseId);

                if (target == null)
                {
                    target = new Inventory { ProductId = line.ProductId, WarehouseId = dto.TargetWarehouseId, Quantity = 0 };
                    _context.Inventories.Add(target);
                }

                target.Quantity += line.Quantity;
                target.LastStockDate = DateTime.UtcNow;

                transfer.Items.Add(new StockTransferItem
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    SourceInventory = source,
                    TargetInventory = target,
                    Notes = dto.Notes
                });

                _context.StockMovements.Add(new StockMovement
                {
                    ProductId = line.ProductId,
                    WarehouseId = line.SourceWarehouseId,
                    Type = StockMovementType.Transfer,
                    Quantity = -line.Quantity,
                    Reference = transfer.TransferNumber,
                    Notes = dto.Notes,
                    MovementDate = date,
                    SourceWarehouseId = line.SourceWarehouseId,
                    DestinationWarehouseId = dto.TargetWarehouseId
                });
                _context.StockMovements.Add(new StockMovement
                {
                    ProductId = line.ProductId,
                    WarehouseId = dto.TargetWarehouseId,
                    Type = StockMovementType.Transfer,
                    Quantity = line.Quantity,
                    Reference = transfer.TransferNumber,
                    Notes = dto.Notes,
                    MovementDate = date,
                    SourceWarehouseId = line.SourceWarehouseId,
                    DestinationWarehouseId = dto.TargetWarehouseId
                });
            }

            await _context.SaveChangesAsync();
            return transfer.TransferNumber;
        }

        /// <summary>
        /// Cancels a posted transfer: reverses its lines back into the source
        /// warehouses and marks the batch <see cref="StockTransferStatus.Cancelled"/>.
        /// Items and movement legs are kept as an audit trail.
        /// </summary>
        public async Task CancelStockTransferAsync(Guid id)
        {
            var transfer = await _context.StockTransfers
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new EntityNotFoundException("StockTransfer", id);

            if (transfer.Status == StockTransferStatus.Cancelled)
                return;

            await ReverseTransferLinesAsync(transfer, "cancel");

            transfer.Status = StockTransferStatus.Cancelled;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds back to the source inventories and subtracts from the target
        /// inventories for every posted line. Used by edit (before re-posting)
        /// and cancel. Throws when stock was already consumed.
        /// </summary>
        private async Task ReverseTransferLinesAsync(StockTransfer transfer, string action)
        {
            foreach (var old in transfer.Items.ToList())
            {
                var source = old.SourceInventoryId.HasValue
                    ? await _context.Inventories.FirstOrDefaultAsync(i => i.Id == old.SourceInventoryId.Value)
                    : await FindInventoryAsync(old.ProductId, transfer.SourceWarehouseId);

                var target = old.TargetInventoryId.HasValue
                    ? await _context.Inventories.FirstOrDefaultAsync(i => i.Id == old.TargetInventoryId.Value)
                    : await FindInventoryAsync(old.ProductId, transfer.TargetWarehouseId);

                if (source == null || target == null)
                    throw new InvalidOperationException($"Cannot {action} transfer {transfer.TransferNumber}: the original stock records no longer exist.");

                if (target.AvailableQuantity < old.Quantity)
                    throw new InvalidOperationException($"Cannot {action} transfer {transfer.TransferNumber}: {old.Quantity} unit(s) of this product have already been used at the target warehouse.");

                source.Quantity += old.Quantity;
                source.LastStockDate = DateTime.UtcNow;
                target.Quantity -= old.Quantity;
                target.LastStockDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Finds an inventory row, checking already-tracked entities first so that a row
        /// added earlier in the same batch (not yet saved) is reused instead of inserted
        /// twice — which would violate the unique index on Tenant + Product + Warehouse.
        /// </summary>
        private async Task<Inventory?> FindInventoryAsync(Guid productId, Guid warehouseId) =>
            _context.Inventories.Local.FirstOrDefault(i => i.ProductId == productId && i.WarehouseId == warehouseId)
            ?? await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);

        public async Task<PagedResultNew<StockTransferListDto>> GetStockTransfersPagedAsync(StockTransferPagedRequest request)
        {
            // Own context for the whole read: the grid can be re-entered while another query on
            // the scoped context is still in flight, and a DbContext cannot run two commands at once.
            await using var read = await _contextFactory.CreateDbContextAsync();
            var response = new PagedResultNew<StockTransferListDto>();

            var page = Math.Max(0, request.CurrentPage);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var search = request.SearchTerm?.Trim();

            IQueryable<StockTransfer> query = read.StockTransfers
                .AsSplitQuery()
                .AsNoTracking()
                .Include(t => t.SourceWarehouse)
                .Include(t => t.TargetWarehouse)
                .Include(t => t.Items).ThenInclude(i => i.Product);

            if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
                query = query.Where(t => t.TenantId == request.TenantId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t =>
                    EF.Functions.Like(t.TransferNumber, $"%{search}%") ||
                    EF.Functions.Like(t.Notes, $"%{search}%") ||
                    EF.Functions.Like(t.SourceWarehouse.Name, $"%{search}%") ||
                    EF.Functions.Like(t.TargetWarehouse.Name, $"%{search}%") ||
                    t.Items.Any(i => EF.Functions.Like(i.Product.Name, $"%{search}%") ||
                                     EF.Functions.Like(i.Product.SKU, $"%{search}%")));

            if (request.Status.HasValue)
                query = query.Where(t => t.Status == request.Status.Value);

            if (request.WarehouseId.HasValue && request.WarehouseId != Guid.Empty)
            {
                var w = request.WarehouseId.Value;
                query = query.Where(t => t.SourceWarehouseId == w || t.TargetWarehouseId == w);
            }

            if (request.From.HasValue)
                query = query.Where(t => t.TransferDate >= request.From.Value.Date);

            if (request.To.HasValue)
            {
                var toExclusive = request.To.Value.Date.AddDays(1);
                query = query.Where(t => t.TransferDate < toExclusive);
            }

            response.Stats["TotalCount"] = await query.CountAsync();
            response.Stats["TotalLines"] = await query.SelectMany(t => t.Items).CountAsync();
            response.Stats["TotalUnits"] = await query.SelectMany(t => t.Items).SumAsync(i => (int?)i.Quantity) ?? 0;
            response.Stats["CompletedCount"] = await query.CountAsync(t => t.Status == StockTransferStatus.Completed);
            response.Stats["WarehouseCount"] = await query.Select(t => t.SourceWarehouseId)
                .Union(query.Select(t => t.TargetWarehouseId))
                .CountAsync();

            var sortAsc = request.SortAscending;
            query = (request.SortColumn ?? "TransferDate") switch
            {
                "TransferNumber" => sortAsc ? query.OrderBy(t => t.TransferNumber) : query.OrderByDescending(t => t.TransferNumber),
                "SourceWarehouse" => sortAsc ? query.OrderBy(t => t.SourceWarehouse.Name) : query.OrderByDescending(t => t.SourceWarehouse.Name),
                "TargetWarehouse" => sortAsc ? query.OrderBy(t => t.TargetWarehouse.Name) : query.OrderByDescending(t => t.TargetWarehouse.Name),
                "TotalQuantity" => sortAsc ? query.OrderBy(t => t.Items.Sum(i => i.Quantity)) : query.OrderByDescending(t => t.Items.Sum(i => i.Quantity)),
                _ => sortAsc ? query.OrderBy(t => t.TransferDate) : query.OrderByDescending(t => t.TransferDate),
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip(page * pageSize).Take(pageSize).ToListAsync();

            response.Items = _mapper.Map<List<StockTransferListDto>>(items);
            response.TotalCount = totalCount;
            response.CurrentPage = page;
            response.PageSize = pageSize;

            return response;
        }

        public async Task<StockTransferListDto> GetStockTransferByIdAsync(Guid id)
        {
            var transfer = await _context.StockTransfers
                .AsSplitQuery()
                .AsNoTracking()
                .Include(t => t.SourceWarehouse)
                .Include(t => t.TargetWarehouse)
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .Include(t => t.Items).ThenInclude(i => i.SourceInventory).ThenInclude(w => w!.Warehouse)
                .FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new EntityNotFoundException("StockTransfer", id);

            return _mapper.Map<StockTransferListDto>(transfer);
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
