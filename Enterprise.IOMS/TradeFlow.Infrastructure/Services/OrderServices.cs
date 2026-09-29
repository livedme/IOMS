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

    public async Task TransferStock(Guid productId, Guid sourceWarehouseId, Guid targetWarehouseId, int quantity)
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
        _context.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            WarehouseId = sourceWarehouseId,
            Type = StockMovementType.Transfer,
            Quantity = -quantity,
            Reference = reference,
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

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAccountingService _accountingService;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public OrderService(ApplicationDbContext context, IMapper mapper, IAccountingService accountingService, IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _context = context;
        _mapper = mapper;
        _accountingService = accountingService;
        _contextFactory = contextFactory;
    }

    public async Task<Guid> CreateSalesOrder(CreateSalesOrderDto dto)
    {
        var order = new SalesOrder
        {
            OrderNumber = NumberGenerator.GenerateOrderNumber("SO"),
            CustomerId = dto.CustomerId,
            WarehouseId = dto.WarehouseId,
            BranchId = dto.BranchId,
            Notes = dto.Notes,
            ShippingAddress = dto.ShippingAddress,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            CurrencyId = dto.CurrencyId,
            OrderDate = dto.SalesDate,
            SubTotal = dto.SubTotal,
            LabourCharge = dto.LabourCharge,
            TruckCharge = dto.TruckCharge,
            TaxAmount = dto.TaxAmount,
            DiscountType = dto.DiscountType,
            DiscountAmount = dto.DiscountAmount,
            TotalAmount = dto.TotalAmount,
            PaidAmount = dto.PaidAmount,
            DueAmount = dto.DueAmount,
            Status = dto.Status == null ? OrderStatus.Pending : dto.Status
        };

        foreach (var item in dto.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId)
                ?? throw new EntityNotFoundException("Product", item.ProductId);

            var taxResult = await CalculateItemTax(item.ProductId, dto.CustomerId, item.UnitPrice * item.Quantity);
            var discountAmt = item.UnitPrice * item.Quantity * (item.DiscountAmount / 100m);
            var lineTotal = (item.UnitPrice * item.Quantity) - discountAmt + taxResult.TaxAmount;

            order.Items.Add(new SalesOrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountType = item.DiscountType,
                DiscountAmount = discountAmt,
                LineTotalPrice = lineTotal,
                UoMId = item.UoMId
            });
        }

        //order.SubTotal = order.Items.Sum(i => i.LineTotalPrice);
        //order.DiscountAmount = order.Items.Sum(i => i.DiscountAmount);
        //order.TaxAmount = order.TaxAmount;
        //order.TotalAmount = order.SubTotal - order.DiscountAmount + order.TaxAmount + order.TruckCharge + order.LabourCharge;

        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync();

        // Mark selected serials as Sold
        foreach (var item in dto.Items.Where(i => i.SerialIds != null && i.SerialIds.Count > 0))
        {
            var serials = await _context.ProductSerials
                .Where(ps => item.SerialIds!.Contains(ps.Id))
                .ToListAsync();
            foreach (var serial in serials)
                serial.Status = ProductSerialStatus.Sold;
        }
        await _context.SaveChangesAsync();

        return order.Id;
    }

    public async Task UpdateSalesOrder(Guid id, CreateSalesOrderDto dto)
    {
        // Clear stale tracked entities from previous operations in the same Blazor circuit scope
        _context.ChangeTracker.Clear();

        var order = await _context.SalesOrders
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new EntityNotFoundException("SalesOrder", id);

        order.CustomerId = dto.CustomerId;
        order.BranchId = dto.BranchId;
        order.Notes = dto.Notes;
        order.OrderDate = dto.SalesDate;
        order.SubTotal = dto.SubTotal;
        order.LabourCharge = dto.LabourCharge;
        order.TruckCharge = dto.TruckCharge;
        order.TaxAmount = dto.TaxAmount;
        order.DiscountType = dto.DiscountType;
        order.DiscountAmount = dto.DiscountAmount;
        order.TotalAmount = dto.TotalAmount;
        order.PaidAmount = dto.PaidAmount;
        order.DueAmount = dto.DueAmount;

        // Load old items (IgnoreQueryFilters bypasses TenantId/IsDeleted global filter)
        // then RemoveRange so EF Core tracks them as Deleted with correct RowVersion
        var oldItems = await _context.SalesOrderItems
            .IgnoreQueryFilters()
            .Where(i => i.SalesOrderId == id)
            .ToListAsync();
        _context.SalesOrderItems.RemoveRange(oldItems);

        foreach (var item in dto.Items)
        {
            _context.SalesOrderItems.Add(new SalesOrderItem
            {
                SalesOrderId = id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountType = item.DiscountType,
                DiscountAmount = item.DiscountAmount,
                TotalDiscount = item.TotalDiscountAmount,
                LineTotalPrice = item.TotalPrice,
                UoMId = item.UoMId
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task ApproveOrder(Guid orderId)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Items)
            .Include(o => o.Branch)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new EntityNotFoundException("SalesOrder", orderId);

        if (order.Status != OrderStatus.Pending)
            throw new DomainException($"Order cannot be approved. Current status: {order.Status}");

        var warehouseId = order.WarehouseId
            ?? (await _context.Warehouses.FirstOrDefaultAsync())?.Id
            ?? throw new DomainException("No warehouse configured");

        foreach (var item in order.Items)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == warehouseId);

            if (inv == null || inv.AvailableQuantity < item.Quantity)
                throw new InsufficientStockException(item.ProductId, item.Quantity, inv?.AvailableQuantity ?? 0);

            inv.Quantity -= item.Quantity;
            inv.LastStockDate = DateTime.UtcNow;

            _context.StockMovements.Add(new StockMovement
            {
                ProductId = item.ProductId,
                WarehouseId = warehouseId,
                Type = StockMovementType.Out,
                Quantity = -item.Quantity,
                Reference = $"Sales Order: {order.OrderNumber}"
            });
        }

        order.Status = OrderStatus.Approved;
        await _context.SaveChangesAsync();
        await _accountingService.AutoPostSalesEntry(order.Id, order.TotalAmount);
    }

    public async Task CancelOrder(Guid orderId, string reason)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Items)
            .Include(o => o.Branch)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new EntityNotFoundException("SalesOrder", orderId);

        if (order.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new DomainException($"Order cannot be cancelled. Current status: {order.Status}");

        var warehouseId = order.WarehouseId
            ?? (await _context.Warehouses.FirstOrDefaultAsync())?.Id;

        if (order.Status == OrderStatus.Approved && warehouseId != null)
        {
            foreach (var item in order.Items)
            {
                var inv = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == warehouseId);
                if (inv != null)
                {
                    inv.Quantity += item.Quantity;
                    inv.LastStockDate = DateTime.UtcNow;

                    _context.StockMovements.Add(new StockMovement
                    {
                        ProductId = item.ProductId,
                        WarehouseId = warehouseId.Value,
                        Type = StockMovementType.In,
                        Quantity = item.Quantity,
                        Reference = $"Cancelled SO: {order.OrderNumber} - {reason}"
                    });
                }
            }
        }

        order.Status = OrderStatus.Cancelled;
        order.Notes = $"{order.Notes}\nCancelled: {reason}";
        await _context.SaveChangesAsync();
    }

    public async Task UpdateOrderStatus(Guid orderId, OrderStatus newStatus)
    {
        var order = await _context.SalesOrders.FindAsync(orderId)
            ?? throw new EntityNotFoundException("SalesOrder", orderId);

        order.Status = newStatus;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSalesOrder(Guid id)
    {
        var order = await _context.SalesOrders.FindAsync(id)
            ?? throw new EntityNotFoundException("SalesOrder", id);
        if (order.Status != OrderStatus.Cancelled)
            throw new DomainException("Only cancelled sales orders can be deleted.");
        order.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<SalesOrderDto?> GetSalesOrderById(Guid id)
    {
        var order = await _context.SalesOrders
            .AsSplitQuery()
            .Include(o => o.Customer)
            .Include(o => o.Branch)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        var model = _mapper.Map<SalesOrderDto>(order);
        //var customer = _mapper.Map<CustomerDto>(order.Customer);

        return order == null ? null : model;
    }

    public async Task<PagedResult<SalesOrderDto>> GetSalesOrders(string? search, List<OrderStatus?> status, int page, int pageSize)
    {
        var query = _context.SalesOrders
            .AsSplitQuery()
            .Include(o => o.Customer)
            .Include(o => o.Branch)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (status != null && status.Any()) query = query.Where(o => status.Contains(o.Status));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.OrderNumber.Contains(search) || o.Customer.CustomerName.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<SalesOrderDto>(_mapper.Map<List<SalesOrderDto>>(items), total, page, pageSize);
    }

    /// <summary>
    /// Server-side paged sales list used by the Sales grid. Mirrors
    /// ProductService.GetProductsAsync: zero-based paging, filterable, sortable,
    /// and returns per-status counts in <see cref="PagedResultNew{T}.Stats"/>.
    /// </summary>
    public async Task<PagedResultNew<SalesOrderDto>> GetSalesOrdersAsync(SalesPagedRequest request)
    {
        // Own context for the whole read: the list can be re-entered while another query on the
        // scoped context is still in flight, and a DbContext cannot run two commands at once.
        await using var read = await _contextFactory.CreateDbContextAsync();
        var response = new PagedResultNew<SalesOrderDto>();

        var page = Math.Max(0, request.CurrentPage);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = request.SearchTerm?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        IQueryable<SalesOrder> query;
        if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
            query = read.SalesOrders.IgnoreQueryFilters().AsNoTracking()
                .Where(o => !o.IsDeleted && o.TenantId == request.TenantId.Value);
        else
            query = read.SalesOrders.AsNoTracking().Where(o => !o.IsDeleted);

        // Base status scope — the page decides which lifecycle states it owns.
        var scope = request.StatusScope is { Count: > 0 }
            ? request.StatusScope
            : Enum.GetValues<OrderStatus>().ToList();
        query = query.Where(o => scope.Contains(o.Status));

        // Stats are computed over the scope + the non-status filters, so the tiles
        // keep showing every bucket while a single status tile is active.
        var statsSource = ApplySalesFilters(query, request, search, hasSearch);

        var groups = await statsSource
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byStatus = groups.ToDictionary(g => g.Status, g => g.Count);
        response.Stats["TotalCount"] = byStatus.Values.Sum();
        foreach (var s in Enum.GetValues<OrderStatus>())
            response.Stats[$"{s}Count"] = byStatus.TryGetValue(s, out var c) ? c : 0;

        // Active status filter
        if (request.Status.HasValue)
        {
            var status = request.Status.Value;
            query = query.Where(o => o.Status == status);
        }

        query = ApplySalesFilters(query, request, search, hasSearch);

        // Sorting — server-side ORDER BY before Skip/Take
        var sortAsc = request.SortAscending;
        query = (request.SortColumn ?? "OrderDate") switch
        {
            "OrderNumber" => sortAsc ? query.OrderBy(o => o.OrderNumber) : query.OrderByDescending(o => o.OrderNumber),
            "Customer" => sortAsc
                ? query.OrderBy(o => o.Customer.CustomerName)
                : query.OrderByDescending(o => o.Customer.CustomerName),
            "Branch" => sortAsc
                ? query.OrderBy(o => o.Branch!.Name)
                : query.OrderByDescending(o => o.Branch!.Name),
            "SubTotal" => sortAsc ? query.OrderBy(o => o.SubTotal) : query.OrderByDescending(o => o.SubTotal),
            "TotalAmount" => sortAsc ? query.OrderBy(o => o.TotalAmount) : query.OrderByDescending(o => o.TotalAmount),
            "PaidAmount" => sortAsc ? query.OrderBy(o => o.PaidAmount) : query.OrderByDescending(o => o.PaidAmount),
            "DueAmount" => sortAsc ? query.OrderBy(o => o.DueAmount) : query.OrderByDescending(o => o.DueAmount),
            "Status" => sortAsc ? query.OrderBy(o => o.Status) : query.OrderByDescending(o => o.Status),
            _ => sortAsc ? query.OrderBy(o => o.OrderDate) : query.OrderByDescending(o => o.OrderDate),
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .AsSplitQuery()
            .Include(o => o.Customer)
            .Include(o => o.Branch)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        response.Items = _mapper.Map<List<SalesOrderDto>>(items);
        response.TotalCount = totalCount;
        response.CurrentPage = page;
        response.PageSize = pageSize;

        return response;
    }

    private static IQueryable<SalesOrder> ApplySalesFilters(
        IQueryable<SalesOrder> query, SalesPagedRequest request, string? search, bool hasSearch)
    {
        if (hasSearch)
        {
            query = query.Where(o =>
                EF.Functions.Like(o.OrderNumber, $"%{search}%") ||
                EF.Functions.Like(o.Customer.CustomerName, $"%{search}%") ||
                EF.Functions.Like(o.Chalan, $"%{search}%") ||
                EF.Functions.Like(o.Naration, $"%{search}%"));
        }

        if (request.CustomerId.HasValue && request.CustomerId != Guid.Empty)
            query = query.Where(o => o.CustomerId == request.CustomerId.Value);

        if (request.BranchId.HasValue && request.BranchId != Guid.Empty)
            query = query.Where(o => o.BranchId == request.BranchId.Value);

        if (request.From.HasValue)
            query = query.Where(o => o.OrderDate >= request.From.Value.Date);

        if (request.To.HasValue)
        {
            var toExclusive = request.To.Value.Date.AddDays(1);
            query = query.Where(o => o.OrderDate < toExclusive);
        }

        return query;
    }

    private async Task<TaxCalculationResult> CalculateItemTax(Guid productId, Guid customerId, decimal amount)
    {
        var product = await _context.Products.FindAsync(productId);
        var customer = await _context.Customers.FindAsync(customerId);

        if (product?.IsTaxExempt == true || customer?.IsTaxExempt == true)
            return new TaxCalculationResult(amount, 0, 0, "Exempt", null);

        var taxRate = await _context.TaxRates
            .Where(t => t.IsActive && t.EffectiveFrom <= DateTime.UtcNow && (t.EffectiveTo == null || t.EffectiveTo >= DateTime.UtcNow))
            .FirstOrDefaultAsync();

        if (taxRate == null)
            return new TaxCalculationResult(amount, 0, 0, null, null);

        var taxAmount = Math.Round(amount * taxRate.Rate / 100m, 2);
        return new TaxCalculationResult(amount, taxAmount, taxRate.Rate, taxRate.Name, taxRate.Id);
    }
}

public class PurchaseService : IPurchaseService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAccountingService _accountingService;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public PurchaseService(ApplicationDbContext context, IMapper mapper, IAccountingService accountingService, IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _context = context;
        _mapper = mapper;
        _accountingService = accountingService;
        _contextFactory = contextFactory;
    }

    public async Task<Guid> CreatePurchaseOrder(CreatePurchaseOrderDto dto)
    {
        var po = new PurchaseOrder
        {
            OrderNumber = NumberGenerator.GenerateOrderNumber("PO"),
            SupplierId = dto.SupplierId,
            WarehouseId = dto.WarehouseId,
            Notes = dto.Notes,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            CurrencyId = dto.CurrencyId,
            Status = PurchaseOrderStatus.Draft
        };

        foreach (var item in dto.Items)
        {
            var lineTotal = item.UnitPrice * item.Quantity;
            po.Items.Add(new PurchaseOrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = lineTotal,
                UoMId = item.UoMId
            });
        }

        po.SubTotal = po.Items.Sum(i => i.LineTotal);
        po.TotalAmount = po.SubTotal + po.TaxAmount;

        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        // Create ProductSerial records for items that have serial entries
        foreach (var item in dto.Items.Where(i => i.Serials != null && i.Serials.Count > 0))
        {
            foreach (var entry in item.Serials!)
            {
                _context.ProductSerials.Add(new ProductSerial
                {
                    ProductId = item.ProductId,
                    SerialNumber = entry.SerialNumber,
                    Barcode = entry.Barcode ?? entry.SerialNumber,
                    Status = ProductSerialStatus.Available,
                    WarehouseId = dto.WarehouseId,
                    SupplierId = dto.SupplierId,
                    PurchaseDate = DateTime.UtcNow,
                    WarrantyStartDate = entry.WarrantyStartDate,
                    WarrantyEndDate = entry.WarrantyEndDate,
                    BinLocation = entry.BinLocation
                });
            }
        }
        await _context.SaveChangesAsync();

        return po.Id;
    }

    public async Task UpdatePurchaseOrder(Guid id, CreatePurchaseOrderDto dto)
    {
        // Clear stale tracked entities from previous operations in the same Blazor circuit scope
        _context.ChangeTracker.Clear();

        var po = await _context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new EntityNotFoundException("PurchaseOrder", id);

        po.SupplierId = dto.SupplierId;
        po.WarehouseId = dto.WarehouseId;
        po.Notes = dto.Notes;
        po.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;

        // Load old items (IgnoreQueryFilters bypasses TenantId/IsDeleted global filter)
        // then RemoveRange so EF Core tracks them as Deleted with correct RowVersion
        var oldItems = await _context.PurchaseOrderItems
            .IgnoreQueryFilters()
            .Where(i => i.PurchaseOrderId == id)
            .ToListAsync();
        _context.PurchaseOrderItems.RemoveRange(oldItems);

        foreach (var item in dto.Items)
        {
            _context.PurchaseOrderItems.Add(new PurchaseOrderItem
            {
                PurchaseOrderId = id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                DiscountType = item.DiscountType,
                TotalDiscount = item.TotalDiscountAmount,
                LineTotal = item.TotalPrice,
                UoMId = item.UoMId
            });
        }

        po.SubTotal = dto.Items.Sum(i => i.TotalPrice);
        po.TotalAmount = po.SubTotal + po.TaxAmount;

        await _context.SaveChangesAsync();
    }

    public async Task ApprovePurchaseOrder(Guid poId)
    {
        var po = await _context.PurchaseOrders.FindAsync(poId)
            ?? throw new EntityNotFoundException("PurchaseOrder", poId);

        if (po.Status != PurchaseOrderStatus.Draft && po.Status != PurchaseOrderStatus.Submitted)
            throw new DomainException($"PO cannot be approved. Current status: {po.Status}");

        po.Status = PurchaseOrderStatus.Approved;
        await _context.SaveChangesAsync();
    }

    public async Task CancelPurchaseOrder(Guid id)
    {
        var po = await _context.PurchaseOrders.FindAsync(id)
            ?? throw new EntityNotFoundException("PurchaseOrder", id);
        if (po.Status != PurchaseOrderStatus.Draft && po.Status != PurchaseOrderStatus.Submitted)
            throw new DomainException($"Cannot cancel a purchase order with status {po.Status}.");
        po.Status = PurchaseOrderStatus.Cancelled;
        await _context.SaveChangesAsync();
    }

    public async Task DeletePurchaseOrder(Guid id)
    {
        var po = await _context.PurchaseOrders.FindAsync(id)
            ?? throw new EntityNotFoundException("PurchaseOrder", id);
        if (po.Status != PurchaseOrderStatus.Draft && po.Status != PurchaseOrderStatus.Cancelled)
            throw new DomainException("Only draft or cancelled purchase orders can be deleted.");
        po.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task ReceiveGoods(Guid poId, List<GoodsReceivedLineDto> lines)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == poId)
            ?? throw new EntityNotFoundException("PurchaseOrder", poId);

        var warehouseId = po.WarehouseId
            ?? (await _context.Warehouses.FirstOrDefaultAsync())?.Id
            ?? throw new DomainException("No warehouse configured");

        foreach (var line in lines)
        {
            var poItem = po.Items.FirstOrDefault(i => i.Id == line.PurchaseOrderItemId)
                ?? throw new DomainException($"PO item {line.PurchaseOrderItemId} not found");

            poItem.ReceivedQuantity += line.ReceivedQuantity;

            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == line.ProductId && i.WarehouseId == warehouseId);
            if (inv == null)
            {
                inv = new Inventory { ProductId = line.ProductId, WarehouseId = warehouseId, Quantity = 0 };
                _context.Inventories.Add(inv);
            }

            inv.Quantity += line.ReceivedQuantity;
            inv.LastStockDate = DateTime.UtcNow;

            _context.StockMovements.Add(new StockMovement
            {
                ProductId = line.ProductId,
                WarehouseId = warehouseId,
                Type = StockMovementType.In,
                Quantity = line.ReceivedQuantity,
                Reference = $"GRN for PO: {po.OrderNumber}"
            });
        }

        var allReceived = po.Items.All(i => i.ReceivedQuantity >= i.Quantity);
        var anyReceived = po.Items.Any(i => i.ReceivedQuantity > 0);
        po.Status = allReceived ? PurchaseOrderStatus.Received
            : anyReceived ? PurchaseOrderStatus.PartiallyReceived
            : po.Status;

        await _context.SaveChangesAsync();
        await _accountingService.AutoPostPurchaseEntry(po.Id, lines.Sum(l => l.ReceivedQuantity * (po.Items.First(i => i.Id == l.PurchaseOrderItemId).UnitPrice)));
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderById(Guid id)
    {
        var po = await _context.PurchaseOrders
            .AsSplitQuery()
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

        return po == null ? null : _mapper.Map<PurchaseOrderDto>(po);
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrders(string? search, PurchaseOrderStatus? status, int page, int pageSize)
    {
        var query = _context.PurchaseOrders
            .AsSplitQuery()
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.OrderNumber.Contains(search) || p.Supplier.SupplierName.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.PurchaseDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<PurchaseOrderDto>(_mapper.Map<List<PurchaseOrderDto>>(items), total, page, pageSize);
    }

    /// <summary>
    /// Server-side paged purchase-order list used by the Purchase Orders grid. Mirrors
    /// GetSalesOrdersAsync: zero-based paging, filterable, sortable, and returns
    /// per-status counts in <see cref="PagedResultNew{T}.Stats"/>.
    /// </summary>
    public async Task<PagedResultNew<PurchaseOrderDto>> GetPurchaseOrdersAsync(PurchaseOrderPagedRequest request)
    {
        // Own context for the whole read: the list can be re-entered while another query on the
        // scoped context is still in flight, and a DbContext cannot run two commands at once.
        await using var read = await _contextFactory.CreateDbContextAsync();
        var response = new PagedResultNew<PurchaseOrderDto>();

        var page = Math.Max(0, request.CurrentPage);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = request.SearchTerm?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        IQueryable<PurchaseOrder> query;
        if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
            query = read.PurchaseOrders.IgnoreQueryFilters().AsNoTracking()
                .Where(p => !p.IsDeleted && p.TenantId == request.TenantId.Value);
        else
            query = read.PurchaseOrders.AsNoTracking().Where(p => !p.IsDeleted);

        // Stats are computed over the non-status filters so the tiles keep showing
        // every bucket while a single status tile is active.
        var statsSource = ApplyPurchaseOrderFilters(query, request, search, hasSearch);

        var groups = await statsSource
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byStatus = groups.ToDictionary(g => g.Status, g => g.Count);
        response.Stats["TotalCount"] = byStatus.Values.Sum();
        foreach (var s in Enum.GetValues<PurchaseOrderStatus>())
            response.Stats[$"{s}Count"] = byStatus.TryGetValue(s, out var c) ? c : 0;

        if (request.Status.HasValue)
        {
            var status = request.Status.Value;
            query = query.Where(p => p.Status == status);
        }

        query = ApplyPurchaseOrderFilters(query, request, search, hasSearch);

        var sortAsc = request.SortAscending;
        query = (request.SortColumn ?? "PurchaseDate") switch
        {
            "OrderNumber" => sortAsc ? query.OrderBy(p => p.OrderNumber) : query.OrderByDescending(p => p.OrderNumber),
            "Supplier" => sortAsc
                ? query.OrderBy(p => p.Supplier.SupplierName)
                : query.OrderByDescending(p => p.Supplier.SupplierName),
            "Warehouse" => sortAsc
                ? query.OrderBy(p => p.Warehouse!.Name)
                : query.OrderByDescending(p => p.Warehouse!.Name),
            "TotalAmount" => sortAsc ? query.OrderBy(p => p.TotalAmount) : query.OrderByDescending(p => p.TotalAmount),
            "DueAmount" => sortAsc ? query.OrderBy(p => p.DueAmount) : query.OrderByDescending(p => p.DueAmount),
            "Status" => sortAsc ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status),
            _ => sortAsc ? query.OrderBy(p => p.PurchaseDate) : query.OrderByDescending(p => p.PurchaseDate),
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .AsSplitQuery()
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        response.Items = _mapper.Map<List<PurchaseOrderDto>>(items);
        response.TotalCount = totalCount;
        response.CurrentPage = page;
        response.PageSize = pageSize;

        return response;
    }

    private static IQueryable<PurchaseOrder> ApplyPurchaseOrderFilters(
        IQueryable<PurchaseOrder> query, PurchaseOrderPagedRequest request, string? search, bool hasSearch)
    {
        if (hasSearch)
        {
            query = query.Where(p =>
                EF.Functions.Like(p.OrderNumber, $"%{search}%") ||
                EF.Functions.Like(p.Supplier.SupplierName, $"%{search}%") ||
                EF.Functions.Like(p.Warehouse!.Name, $"%{search}%") ||
                EF.Functions.Like(p.Notes, $"%{search}%"));
        }

        if (request.SupplierId.HasValue && request.SupplierId != Guid.Empty)
            query = query.Where(p => p.SupplierId == request.SupplierId.Value);

        if (request.WarehouseId.HasValue && request.WarehouseId != Guid.Empty)
            query = query.Where(p => p.WarehouseId == request.WarehouseId.Value);

        if (request.From.HasValue)
            query = query.Where(p => p.PurchaseDate >= request.From.Value.Date);

        if (request.To.HasValue)
        {
            var toExclusive = request.To.Value.Date.AddDays(1);
            query = query.Where(p => p.PurchaseDate < toExclusive);
        }

        return query;
    }
}

public class BranchService : IBranchService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public BranchService(ApplicationDbContext context, IMapper mapper, IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _context = context;
        _mapper = mapper;
        _contextFactory = contextFactory;
    }

    public async Task<PagedResultNew<BranchDto>> GetBranchesPagedAsync(BranchPagedRequest request)
    {
        // Own context for the whole read: the list can be re-entered while another query on the
        // scoped context is still in flight, and a DbContext cannot run two commands at once.
        await using var read = await _contextFactory.CreateDbContextAsync();
        var response = new PagedResultNew<BranchDto>();

        var page = Math.Max(0, request.CurrentPage);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = request.SearchTerm?.Trim();

        var query = read.Branches.AsNoTracking();
        if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
            query = query.Where(b => b.TenantId == request.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b =>
                EF.Functions.Like(b.Name, $"%{search}%") ||
                EF.Functions.Like(b.Code, $"%{search}%") ||
                EF.Functions.Like(b.Location, $"%{search}%") ||
                EF.Functions.Like(b.Address, $"%{search}%"));
        }

        // Stats ignore the active filter so the tiles keep showing both buckets.
        var statRows = await query
            .GroupBy(b => b.IsActive)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        response.Stats["TotalCount"] = statRows.Sum(r => r.Count);
        response.Stats["ActiveCount"] = statRows.Where(r => r.Key).Sum(r => r.Count);
        response.Stats["InactiveCount"] = statRows.Where(r => !r.Key).Sum(r => r.Count);
        response.Stats["WithLocationCount"] = await query
            .CountAsync(b => b.Location != null && b.Location != string.Empty);
        response.Stats["UniqueCodeCount"] = await query.Select(b => b.Code).Distinct().CountAsync();

        if (request.IsActive.HasValue)
            query = query.Where(b => b.IsActive == request.IsActive.Value);

        var sortAsc = request.SortAscending;
        query = (request.SortColumn ?? "Name") switch
        {
            "Code" => sortAsc ? query.OrderBy(b => b.Code) : query.OrderByDescending(b => b.Code),
            "Location" => sortAsc ? query.OrderBy(b => b.Location) : query.OrderByDescending(b => b.Location),
            "Address" => sortAsc ? query.OrderBy(b => b.Address) : query.OrderByDescending(b => b.Address),
            "IsActive" => sortAsc ? query.OrderBy(b => b.IsActive) : query.OrderByDescending(b => b.IsActive),
            _ => sortAsc ? query.OrderBy(b => b.Name) : query.OrderByDescending(b => b.Name),
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        response.Items = items
            .Select(b => new BranchDto(b.Id, b.Name, b.Code, b.Location, b.Address, b.IsActive))
            .ToList();
        response.TotalCount = totalCount;
        response.CurrentPage = page;
        response.PageSize = pageSize;

        return response;
    }
    public async Task<Guid> CreateBranch(CreateBranchDto dto)
    {

        var branch = new Branch()
        {
            Name = dto.Name,
            Code = dto.Code,
            Location = dto.Location,
            Address = dto.Address,
            IsActive = dto.IsActive
        };

        await _context.Branches.AddAsync(branch);
        await _context.SaveChangesAsync();

        return branch.Id;
    }

    public async Task DeleteBranch(Guid id)
    {
        var entity = await _context.Branches.FindAsync(id) ?? throw new EntityNotFoundException("Branches", id);
        if (entity != null)
        {
            _context.Branches.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<BranchDto?> GetBranchById(Guid id)
    {
        var query = await _context.Branches
           .Include(o => o.SalesOrders).FirstOrDefaultAsync(o => o.Id == id) ?? throw new EntityNotFoundException("Branches", id);

        var response = _mapper.Map<BranchDto>(query);

        return response;
    }

    public async Task<List<BranchDto>> GetBranches()
    {
        var query = _context.Branches
            .Include(o => o.SalesOrders)
            .AsQueryable();

        var response = _mapper.Map<List<BranchDto>>(query.ToList());

        return response ?? new List<BranchDto>();
    }

    public async Task UpdateBranch(Guid id, CreateBranchDto dto)
    {
        var entity = await _context.Branches.FindAsync(id) ?? throw new EntityNotFoundException("Branches", id);
        if (entity != null)
        {
            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.Location = dto.Location;
            entity.Address = dto.Address;
            entity.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();
        }
    }
}
