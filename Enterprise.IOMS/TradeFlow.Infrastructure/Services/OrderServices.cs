using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace TradeFlow.Infrastructure.Services
{
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
                DiscountType = (DiscountType)dto.DiscountType,
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
                    DiscountType = (DiscountType)item.DiscountType,
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
            order.DiscountType = (DiscountType)dto.DiscountType;
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
                    DiscountType = (DiscountType)item.DiscountType,
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

            // Projected, not Include()d: the grid renders order header fields plus a line count,
            // so materialising the full item graph joined to products cost ~5x the time (and two
            // extra round trips under AsSplitQuery) for data no column shows. The drawer still gets
            // its lines from GetSalesOrderById, which loads the graph for a single order.
            var rows = await query
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.CustomerId,
                    CustomerName = o.Customer.CustomerName,
                    o.BranchId,
                    BranchName = o.Branch == null ? null : o.Branch.Name,
                    o.OrderDate,
                    o.Status,
                    o.Naration,
                    o.Chalan,
                    o.SubTotal,
                    o.TruckCharge,
                    o.LabourCharge,
                    o.TaxAmount,
                    o.DiscountAmount,
                    o.DiscountType,
                    o.TotalAmount,
                    o.PaidAmount,
                    o.DueAmount,
                    o.Notes,
                    o.ExchangeRate,
                    o.ExpectedDeliveryDate,
                    ItemCount = o.Items.Count,
                })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = rows
                .Select(o => new SalesOrderDto(
                    o.Id,
                    o.OrderNumber,
                    o.CustomerId,
                    new CustomerDto { Id = o.CustomerId, CustomerName = o.CustomerName },
                    o.BranchId,
                    o.BranchId.HasValue
                        ? new BranchDto(o.BranchId.Value, o.BranchName, null, null, null, true)
                        : null,
                    o.OrderDate,
                    o.Status,
                    o.Naration,
                    o.Chalan,
                    o.SubTotal,
                    o.TruckCharge,
                    o.LabourCharge,
                    o.TaxAmount,
                    o.DiscountAmount,
                    o.DiscountType,
                    o.TotalAmount,
                    o.PaidAmount,
                    o.DueAmount,
                    o.Notes,
                    o.ExchangeRate,
                    o.ExpectedDeliveryDate,
                    new List<SalesOrderItemDto>(),
                    o.ItemCount))
                .ToList();

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
}


