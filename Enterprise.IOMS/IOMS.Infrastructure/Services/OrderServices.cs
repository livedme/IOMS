using AutoMapper;
using Humanizer;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Domain.Exceptions;
using IOMS.Infrastructure.Data;
using IOMS.Shared.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public InventoryService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
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

    public OrderService(ApplicationDbContext context, IMapper mapper, IAccountingService accountingService)
    {
        _context = context;
        _mapper = mapper;
        _accountingService = accountingService;
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

    public async Task<SalesOrderDto?> GetSalesOrderById(Guid id)
    {
        var order = await _context.SalesOrders
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
            .Include(o => o.Customer)
            .Include(o => o.Branch)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (status != null && status.Any()) query = query.Where(o => status.Contains(o.Status));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.OrderNumber.Contains(search) || o.Customer.Name.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<SalesOrderDto>(_mapper.Map<List<SalesOrderDto>>(items), total, page, pageSize);
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

    public PurchaseService(ApplicationDbContext context, IMapper mapper, IAccountingService accountingService)
    {
        _context = context;
        _mapper = mapper;
        _accountingService = accountingService;
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

    public async Task ApprovePurchaseOrder(Guid poId)
    {
        var po = await _context.PurchaseOrders.FindAsync(poId)
            ?? throw new EntityNotFoundException("PurchaseOrder", poId);

        if (po.Status != PurchaseOrderStatus.Draft && po.Status != PurchaseOrderStatus.Submitted)
            throw new DomainException($"PO cannot be approved. Current status: {po.Status}");

        po.Status = PurchaseOrderStatus.Approved;
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
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

        return po == null ? null : _mapper.Map<PurchaseOrderDto>(po);
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrders(string? search, PurchaseOrderStatus? status, int page, int pageSize)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.OrderNumber.Contains(search) || p.Supplier.Name.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.PurchaseDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<PurchaseOrderDto>(_mapper.Map<List<PurchaseOrderDto>>(items), total, page, pageSize);
    }
}

public class BranchService : IBranchService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public BranchService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
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
