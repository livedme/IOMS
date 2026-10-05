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

            // Projected, not Include()d: the grid renders PO header fields only, so materialising the
            // full item graph joined to products cost ~5x the time (and two extra round trips under
            // AsSplitQuery) for data no column shows. The drawer and Receive Goods still load lines
            // via GetPurchaseOrderById, which builds the graph for a single order.
            var items = await query
                .Select(p => new
                {
                    p.Id,
                    p.OrderNumber,
                    p.SupplierId,
                    SupplierName = p.Supplier.SupplierName,
                    p.WarehouseId,
                    WarehouseName = p.Warehouse == null ? null : p.Warehouse.Name,
                    p.PurchaseDate,
                    p.Status,
                    p.SubTotal,
                    p.LabourCharge,
                    p.TruckCharge,
                    p.DiscountAmount,
                    p.DiscountType,
                    p.TotalAmount,
                    p.TaxAmount,
                    p.PaidAmount,
                    p.DueAmount,
                    p.Notes,
                    p.ExpectedDeliveryDate,
                })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = items
                .Select(p => new PurchaseOrderDto(
                    p.Id,
                    p.OrderNumber,
                    p.SupplierId,
                    p.SupplierName,
                    p.WarehouseId ?? Guid.Empty,
                    p.WarehouseName ?? string.Empty,
                    p.PurchaseDate,
                    p.Status,
                    p.SubTotal,
                    p.LabourCharge,
                    p.TruckCharge,
                    p.DiscountAmount,
                    p.DiscountType,
                    p.TotalAmount,
                    p.TaxAmount,
                    p.PaidAmount,
                    p.DueAmount,
                    p.Notes,
                    p.ExpectedDeliveryDate,
                    new List<PurchaseOrderItemDto>()))
                .ToList();

            response.TotalCount = totalCount;
            response.CurrentPage = page;
            response.PageSize = pageSize;

            return response;
        }

        /// <summary>
        /// Newest purchase orders for the dropdowns that only need the number. LandedCost.razor and
        /// PurchaseReturnDialog.razor each loaded the tracked entity — items and all — for this.
        /// </summary>
        public async Task<List<PurchaseOrderOptionDto>> GetRecentPurchaseOrders(int count)
        {
            return await _context.PurchaseOrders
                .AsNoTracking()
                .OrderByDescending(p => p.PurchaseDate)
                .Take(count)
                .Select(p => new PurchaseOrderOptionDto(
                    p.Id,
                    p.OrderNumber,
                    p.PurchaseDate,
                    p.Status))
                .ToListAsync();
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
}
