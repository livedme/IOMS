using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Constants;

namespace TradeFlow.Infrastructure.Identity;

/// <summary>
/// Seeds high-volume transactional data: 20 000 sales orders and 20 000 purchase
/// orders, each with realistic line items, for performance testing of the list pages.
///
/// Distinct from <see cref="SeedData.InitializeLoadTestDataAsync"/>, which is a
/// multi-tenant header-only fixture. This seeder targets a single tenant and writes
/// full order + line-item graphs.
///
/// Opt-in via configuration:  "Seed": { "VolumeData": true }
/// </summary>
public static class VolumeDataSeeder
{
    public const int SalesOrderTarget = 20_000;
    public const int PurchaseOrderTarget = 20_000;

    private const int OrderBatchSize = 250;
    private const int RandomSeed = 20260903;
    private const int MaxDaysBack = 730;
    private const decimal DefaultTaxRate = 10m;
    /// <summary>
    /// Ensures the tenant has the lookup data required to build orders, then tops up
    /// sales and purchase orders to their targets. Safe to run repeatedly — existing
    /// counts are respected, so a second run is a no-op.
    /// </summary>
    /// <param name="serviceProvider">Root service provider; a scope is created internally.</param>
    /// <param name="log">Optional progress sink.</param>
    /// <param name="backfillLineItems">
    /// When true, any existing order that has no line items gets 1-3 generated items
    /// and its SubTotal/Tax/Total recomputed from them. The original load-test seeder
    /// wrote header-only rows, so this fills in the line items those orders lack.
    /// </param>
    /// <param name="salesTarget">Sales order target; defaults to <see cref="SalesOrderTarget"/>.</param>
    /// <param name="purchaseTarget">Purchase order target; defaults to <see cref="PurchaseOrderTarget"/>.</param>
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        Action<string>? log = null,
        bool backfillLineItems = true,
        int salesTarget = SalesOrderTarget,
        int purchaseTarget = PurchaseOrderTarget)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        //log?.Invoke("Applying migrations...");
       // await context.Database.MigrateAsync();

        var tenantId = Guid.Parse(AppConstants.DefaultTenantId);

        // Keep explicit TenantId and skip per-entity audit rows — 40 000 orders plus
        // their line items would otherwise generate hundreds of thousands of audit rows.
        context.DisableAuditLogging = true;
        try
        {
            var lookups = await EnsurePrerequisitesAsync(context, tenantId, log);

            if (backfillLineItems)
            {
                await BackfillSalesLineItemsAsync(context, tenantId, lookups, log);
                await BackfillPurchaseLineItemsAsync(context, tenantId, lookups, log);
            }

            await SeedSalesOrdersAsync(context, tenantId, lookups, salesTarget, log);
            await SeedPurchaseOrdersAsync(context, tenantId, lookups, purchaseTarget, log);
        }
        finally
        {
            context.DisableAuditLogging = false;
        }
    }

    // ── Line-item backfill for header-only orders ─────────────────────────────

    private static async Task BackfillSalesLineItemsAsync(
        ApplicationDbContext context, Guid tenantId, OrderLookups lookups, Action<string>? log)
    {
        const string createdBy = "volume-seed";

        var emptyOrderIds = await context.SalesOrders.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && !o.Items.Any())
            .Select(o => o.Id)
            .ToListAsync();

        if (emptyOrderIds.Count == 0)
        {
            log?.Invoke("Sales orders: all already have line items.");
            return;
        }

        log?.Invoke($"Backfilling line items for {emptyOrderIds.Count} sales order(s)...");

        var random = new Random(RandomSeed + 100);
        var done = 0;

        foreach (var batch in emptyOrderIds.Chunk(OrderBatchSize))
        {
            var orders = await context.SalesOrders.IgnoreQueryFilters()
                .Where(o => batch.Contains(o.Id))
                .ToListAsync();

            var buffer = new List<SalesOrderItem>();
            foreach (var order in orders)
            {
                var items = BuildSalesItems(random, lookups, order.Status, createdBy, tenantId);
                order.Items = items;
                order.SubTotal = Math.Round(items.Sum(i => Math.Round(i.UnitPrice * i.Quantity, 2)), 2);
                var discount = Math.Round(items.Sum(i => i.TotalDiscount), 2);
                order.DiscountAmount = discount;
                var net = Math.Round(order.SubTotal - discount, 2);
                order.TaxAmount = Math.Round(net * DefaultTaxRate / 100m, 2);
                order.TotalAmount = Math.Round(net + order.TaxAmount, 2);
                order.DueAmount = Math.Round(order.TotalAmount - order.PaidAmount, 2);
                buffer.AddRange(items);
            }

            await AddInBatchesAsync(context, buffer, OrderBatchSize);
            done += orders.Count;

            // Header totals were updated in place, so persist those separately.
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            log?.Invoke($"  sales backfill: {done}/{emptyOrderIds.Count}");
        }
    }

    private static async Task BackfillPurchaseLineItemsAsync(
        ApplicationDbContext context, Guid tenantId, OrderLookups lookups, Action<string>? log)
    {
        const string createdBy = "volume-seed";

        var emptyOrderIds = await context.PurchaseOrders.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && !o.Items.Any())
            .Select(o => o.Id)
            .ToListAsync();

        if (emptyOrderIds.Count == 0)
        {
            log?.Invoke("Purchase orders: all already have line items.");
            return;
        }

        log?.Invoke($"Backfilling line items for {emptyOrderIds.Count} purchase order(s)...");

        var random = new Random(RandomSeed + 200);
        var done = 0;

        foreach (var batch in emptyOrderIds.Chunk(OrderBatchSize))
        {
            var orders = await context.PurchaseOrders.IgnoreQueryFilters()
                .Where(o => batch.Contains(o.Id))
                .ToListAsync();

            var buffer = new List<PurchaseOrderItem>();
            foreach (var order in orders)
            {
                var items = BuildPurchaseItems(random, lookups, order.Status, createdBy, tenantId);
                order.Items = items;
                order.SubTotal = Math.Round(items.Sum(i => Math.Round(i.UnitPrice * i.Quantity, 2)), 2);
                var discount = Math.Round(items.Sum(i => i.TotalDiscount), 2);
                order.DiscountAmount = discount;
                var tax = Math.Round(items.Sum(i => i.TaxAmount), 2);
                order.TaxAmount = tax;
                order.TotalAmount = Math.Round(order.SubTotal - discount + tax, 2);
                order.DueAmount = Math.Round(order.TotalAmount - order.PaidAmount, 2);
                buffer.AddRange(items);
            }

            await AddInBatchesAsync(context, buffer, OrderBatchSize);

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            done += orders.Count;
            log?.Invoke($"  purchase backfill: {done}/{emptyOrderIds.Count}");
        }
    }

    private static List<SalesOrderItem> BuildSalesItems(
        Random random, OrderLookups lookups, OrderStatus status, string createdBy, Guid tenantId)
    {
        var lineCount = random.Next(1, 4);
        var items = new List<SalesOrderItem>(lineCount);

        for (var line = 0; line < lineCount; line++)
        {
            var productIndex = random.Next(lookups.ProductIds.Count);
            var quantity = random.Next(1, 25);
            var unitPrice = lookups.SellingPrices[productIndex];
            var gross = Math.Round(unitPrice * quantity, 2);
            var discount = Math.Round(gross * 0.05m, 2);

            int shipped = status switch
            {
                OrderStatus.Delivered or OrderStatus.Packed => quantity,
                OrderStatus.Shipped or OrderStatus.PartiallyShipped => random.Next(0, quantity + 1),
                _ => 0
            };

            items.Add(new SalesOrderItem
            {
                ProductId = lookups.ProductIds[productIndex],
                Quantity = quantity,
                ShippedQuantity = shipped,
                UnitPrice = unitPrice,
                DiscountAmount = discount,
                DiscountType = DiscountType.Fixed,
                TotalDiscount = discount,
                LineTotalPrice = Math.Round(gross - discount, 2),
                UoMId = lookups.UoMId,
                ReturnedQuantity = 0,
                IsBackOrdered = false,
                TenantId = tenantId,
                CreatedBy = createdBy
            });
        }

        return items;
    }

    private static List<PurchaseOrderItem> BuildPurchaseItems(
        Random random, OrderLookups lookups, PurchaseOrderStatus status, string createdBy, Guid tenantId)
    {
        var lineCount = random.Next(1, 4);
        var items = new List<PurchaseOrderItem>(lineCount);

        for (var line = 0; line < lineCount; line++)
        {
            var productIndex = random.Next(lookups.ProductIds.Count);
            var quantity = random.Next(10, 100);
            var unitPrice = lookups.CostPrices[productIndex];
            var gross = Math.Round(unitPrice * quantity, 2);
            var discount = Math.Round(gross * 0.03m, 2);
            var tax = Math.Round((gross - discount) * DefaultTaxRate / 100m, 2);

            var received = status switch
            {
                PurchaseOrderStatus.Received or PurchaseOrderStatus.Closed => quantity,
                PurchaseOrderStatus.PartiallyReceived => random.Next(0, quantity),
                _ => 0
            };

            items.Add(new PurchaseOrderItem
            {
                ProductId = lookups.ProductIds[productIndex],
                Quantity = quantity,
                ReceivedQuantity = received,
                UnitPrice = unitPrice,
                TaxRate = DefaultTaxRate,
                DiscountAmount = discount,
                DiscountType = DiscountType.Fixed,
                TotalDiscount = discount,
                TaxAmount = tax,
                LineTotal = Math.Round(gross - discount + tax, 2),
                UoMId = lookups.UoMId,
                TenantId = tenantId,
                CreatedBy = createdBy
            });
        }

        return items;
    }

    // ── Lookup data ────────────────────────────────────────────────────────────

    private sealed record OrderLookups(
        List<Guid> WarehouseIds,
        List<Guid> CustomerIds,
        List<Guid> SupplierIds,
        List<Guid> ProductIds,
        List<decimal> CostPrices,
        List<decimal> SellingPrices,
        Guid? UoMId);

    private static async Task<OrderLookups> EnsurePrerequisitesAsync(
        ApplicationDbContext context, Guid tenantId, Action<string>? log)
    {
        const string createdBy = "volume-seed";

        if (!await context.Warehouses.IgnoreQueryFilters().AnyAsync(w => w.TenantId == tenantId))
        {
            context.Warehouses.Add(new Warehouse
            {
                Name = "Main Warehouse",
                Code = "WH-001",
                Location = "Central",
                TenantId = tenantId,
                CreatedBy = createdBy
            });
        }

        if (!await context.Branches.IgnoreQueryFilters().AnyAsync(b => b.TenantId == tenantId))
        {
            context.Branches.Add(new Branch
            {
                Name = "Head Office",
                Code = "BR-001",
                Location = "Head Office",
                TenantId = tenantId,
                CreatedBy = createdBy
            });
        }

        if (!await context.Customers.IgnoreQueryFilters().AnyAsync(c => c.TenantId == tenantId))
        {
            string[] names =
            [
                "Acme Corp", "Global Industries", "TechStart LLC", "Northwind Traders",
                "Contoso Retail", "Fabrikam Supply", "Litware Inc", "Proseware Ltd",
                "Adventure Works", "Tailspin Toys", "Wide World Imports", "Blue Yonder",
                "Fourth Coffee", "Graphic Design Institute", "Humongous Insurance", "IKEA Wholesale",
                "Lucerne Publishing", "Margie's Travel", "Nod Publishers", "Southridge Video"
            ];
            for (var i = 0; i < names.Length; i++)
            {
                context.Customers.Add(new Customer
                {
                    CustomerName = names[i],
                    CustomerEmail = $"sales@{names[i].Replace(" ", "").ToLowerInvariant()}.com",
                    CustomerPhone = $"+1-555-{1000 + i:D4}",
                    Address = $"{100 + i * 7} Commerce Street",
                    City = "New York",
                    State = "NY",
                    Country = "US",
                    PostalCode = $"{10001 + i * 13}",
                    CreditLimit = 50_000m,
                    PaymentTerms = "Net 30",
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = createdBy
                });
            }
        }

        if (!await context.Suppliers.IgnoreQueryFilters().AnyAsync(s => s.TenantId == tenantId))
        {
            string[] names =
            [
                "TechParts Intl", "OfficePlus Co", "FurniPro Mfg", "SteelWorks Ltd",
                "ChemSupply Group", "PackRight Inc", "ToolTech Ltd", "EletricPro Co",
                "TextileSource", "PlasticWorks", "GlassLine Ltd", "PaperMill Co",
                "MetalWorks Inc", "ChemCore Ltd", "SafeGuard Supply", "AutoParts Direct",
                "BuildMart", "Industrial Depot", "PrimeSource", "BulkGoods Trading"
            ];
            for (var i = 0; i < names.Length; i++)
            {
                context.Suppliers.Add(new Supplier
                {
                    SupplierName = names[i],
                    SupplierEmail = $"orders@{names[i].Replace(" ", "").ToLowerInvariant()}.com",
                    SupplierPhone = $"+1-555-{2000 + i:D4}",
                    ContactPersonName = $"Contact {i + 1}",
                    Address = $"{200 + i * 11} Industrial Way",
                    City = "Chicago",
                    State = "IL",
                    Country = "US",
                    PostalCode = $"{60601 + i * 17}",
                    PaymentTerms = "Net 45",
                    LeadTimeDays = 7,
                    Rating = 4.0m,
                    IsActive = true,
                    TenantId = tenantId,
                    CreatedBy = createdBy
                });
            }
        }

        if (!await context.Categories.IgnoreQueryFilters().AnyAsync(c => c.TenantId == tenantId))
        {
            context.Categories.Add(new Category
            {
                Name = "General",
                Description = "Default product category",
                TenantId = tenantId,
                CreatedBy = createdBy
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Products are the FK target for every line item. Project to primitives so the
        // change tracker stays empty while tens of thousands of orders are built.
        var products = await context.Products.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.SKU)
            .Select(p => new { p.Id, p.CostPrice, p.SellingPrice })
            .ToListAsync();

        if (products.Count == 0)
        {
            var category = await context.Categories.IgnoreQueryFilters()
                .Where(c => c.TenantId == tenantId).OrderBy(c => c.Name).FirstAsync();
            var random = new Random(RandomSeed);

            for (var i = 0; i < 200; i++)
            {
                var cost = Math.Round((decimal)(random.NextDouble() * 490 + 10), 2);
                context.Products.Add(new Product
                {
                    Name = $"Volume Product {i + 1:D4}",
                    SKU = $"VOL-{i + 1:D5}",
                    Barcode = $"991{i + 1:D10}",
                    Description = "Synthetic product for volume testing",
                    CostPrice = cost,
                    SellingPrice = Math.Round(cost * 1.35m, 2),
                    WholeSellingPrice = Math.Round(cost * 1.2m, 2),
                    ReorderStockLevel = random.Next(5, 50),
                    MinOrderQuantity = 1,
                    CategoryId = category.Id,
                    Unit = "Each",
                    TenantId = tenantId,
                    CreatedBy = createdBy
                });
            }

            await AddInBatchesAsync(context, context.Products.Local.ToList(), OrderBatchSize);

            products = await context.Products.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId)
                .OrderBy(p => p.SKU)
                .Select(p => new { p.Id, p.CostPrice, p.SellingPrice })
                .ToListAsync();
        }

        var warehouseIds = await context.Warehouses.IgnoreQueryFilters()
            .Where(w => w.TenantId == tenantId).Select(w => w.Id).ToListAsync();
        var customerIds = await context.Customers.IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId).Select(c => c.Id).ToListAsync();
        var supplierIds = await context.Suppliers.IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId).Select(s => s.Id).ToListAsync();
        var branchId = await context.Branches.IgnoreQueryFilters()
            .Where(b => b.TenantId == tenantId).Select(b => (Guid?)b.Id).FirstOrDefaultAsync();
        var uomId = await context.UnitsOfMeasure.IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId).Select(u => (Guid?)u.Id).FirstOrDefaultAsync();

        if (warehouseIds.Count == 0 || customerIds.Count == 0 || supplierIds.Count == 0 || products.Count == 0)
            throw new InvalidOperationException("Volume seeding requires a warehouse, customer, supplier and product.");

        log?.Invoke($"Lookups ready: {warehouseIds.Count} warehouse(s), {customerIds.Count} customer(s), " +
                    $"{supplierIds.Count} supplier(s), {products.Count} product(s).");

        return new OrderLookups(
            warehouseIds, customerIds, supplierIds,
            products.Select(p => p.Id).ToList(),
            products.Select(p => p.CostPrice).ToList(),
            products.Select(p => p.SellingPrice).ToList(),
            uomId);
    }

    // ── Sales orders ──────────────────────────────────────────────────────────

    private static async Task SeedSalesOrdersAsync(
        ApplicationDbContext context, Guid tenantId, OrderLookups lookups, int target, Action<string>? log)
    {
        const string createdBy = "volume-seed";

        var existing = await context.SalesOrders.IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId).CountAsync();

        if (existing >= target)
        {
            log?.Invoke($"Sales orders already at target ({existing}).");
            return;
        }

        log?.Invoke($"Seeding sales orders {existing + 1}..{target}...");

        var random = new Random(RandomSeed);
        var statuses = new[]
        {
            OrderStatus.Pending, OrderStatus.Approved, OrderStatus.Sold, OrderStatus.Packed,
            OrderStatus.Shipped, OrderStatus.PartiallyShipped, OrderStatus.Delivered, OrderStatus.Cancelled
        };

        var buffer = new List<SalesOrder>(OrderBatchSize);

        for (var index = existing; index < target; index++)
        {
            var order = BuildSalesOrder(index, tenantId, createdBy, lookups, random, statuses);
            buffer.Add(order);

            if (buffer.Count < OrderBatchSize) continue;

            await AddInBatchesAsync(context, buffer, OrderBatchSize);
            log?.Invoke($"  sales: {index + 1}/{target}");
            buffer.Clear();
        }

        if (buffer.Count > 0)
        {
            await AddInBatchesAsync(context, buffer, OrderBatchSize);
            log?.Invoke($"  sales: {target}/{target}");
        }
    }

    private static SalesOrder BuildSalesOrder(
        int index, Guid tenantId, string createdBy, OrderLookups lookups, Random random, OrderStatus[] statuses)
    {
        var orderDate = DateTime.UtcNow.AddDays(-random.Next(0, MaxDaysBack));
        var status = statuses[random.Next(statuses.Length)];

        var items = BuildSalesItems(random, lookups, status, createdBy, tenantId);
        var subTotal = Math.Round(items.Sum(i => Math.Round(i.UnitPrice * i.Quantity, 2)), 2);
        var totalDiscount = Math.Round(items.Sum(i => i.TotalDiscount), 2);
        var netSubTotal = Math.Round(subTotal - totalDiscount, 2);
        var taxAmount = Math.Round(netSubTotal * DefaultTaxRate / 100m, 2);
        var total = Math.Round(netSubTotal + taxAmount, 2);

        // ~70% fully paid, the rest split between partial and fully outstanding.
        var paidRatio = random.NextDouble() switch
        {
            < 0.70 => 1.0,
            < 0.85 => 0.5,
            _ => 0.0
        };
        var paid = Math.Round(total * (decimal)paidRatio, 2);

        return new SalesOrder
        {
            OrderNumber = $"SO-{index + 1:D6}",
            CustomerId = lookups.CustomerIds[random.Next(lookups.CustomerIds.Count)],
            WarehouseId = lookups.WarehouseIds[random.Next(lookups.WarehouseIds.Count)],
            OrderDate = orderDate,
            Status = status,
            Naration = status == OrderStatus.Cancelled ? "Cancelled during seeding" : string.Empty,
            Chalan = $"CH-{index + 1:D6}",
            SubTotal = subTotal,
            LabourCharge = 0m,
            TruckCharge = 0m,
            TaxAmount = taxAmount,
            DiscountType = DiscountType.Fixed,
            DiscountAmount = totalDiscount,
            TotalAmount = total,
            PaidAmount = paid,
            DueAmount = Math.Round(total - paid, 2),
            ExchangeRate = 1m,
            ExpectedDeliveryDate = orderDate.AddDays(random.Next(3, 30)),
            DeliveredDate = status == OrderStatus.Delivered ? orderDate.AddDays(random.Next(1, 30)) : null,
            Items = items,
            TenantId = tenantId,
            CreatedBy = createdBy
        };
    }

    // ── Purchase orders ───────────────────────────────────────────────────────

    private static async Task SeedPurchaseOrdersAsync(
        ApplicationDbContext context, Guid tenantId, OrderLookups lookups, int target, Action<string>? log)
    {
        const string createdBy = "volume-seed";

        var existing = await context.PurchaseOrders.IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId).CountAsync();

        if (existing >= target)
        {
            log?.Invoke($"Purchase orders already at target ({existing}).");
            return;
        }

        log?.Invoke($"Seeding purchase orders {existing + 1}..{target}...");

        var random = new Random(RandomSeed + 1);
        var statuses = new[]
        {
            PurchaseOrderStatus.Draft, PurchaseOrderStatus.Submitted, PurchaseOrderStatus.Approved,
            PurchaseOrderStatus.PartiallyReceived, PurchaseOrderStatus.Received,
            PurchaseOrderStatus.Closed, PurchaseOrderStatus.Cancelled
        };

        var buffer = new List<PurchaseOrder>(OrderBatchSize);

        for (var index = existing; index < target; index++)
        {
            var order = BuildPurchaseOrder(index, tenantId, createdBy, lookups, random, statuses);
            buffer.Add(order);

            if (buffer.Count < OrderBatchSize) continue;

            await AddInBatchesAsync(context, buffer, OrderBatchSize);
            log?.Invoke($"  purchase: {index + 1}/{target}");
            buffer.Clear();
        }

        if (buffer.Count > 0)
        {
            await AddInBatchesAsync(context, buffer, OrderBatchSize);
            log?.Invoke($"  purchase: {target}/{target}");
        }
    }

    private static PurchaseOrder BuildPurchaseOrder(
        int index, Guid tenantId, string createdBy, OrderLookups lookups, Random random, PurchaseOrderStatus[] statuses)
    {
        var purchaseDate = DateTime.UtcNow.AddDays(-random.Next(0, MaxDaysBack));
        var status = statuses[random.Next(statuses.Length)];

        var items = BuildPurchaseItems(random, lookups, status, createdBy, tenantId);
        var subTotal = Math.Round(items.Sum(i => Math.Round(i.UnitPrice * i.Quantity, 2)), 2);
        var totalDiscount = Math.Round(items.Sum(i => i.TotalDiscount), 2);
        var totalTax = Math.Round(items.Sum(i => i.TaxAmount), 2);
        var total = Math.Round(subTotal - totalDiscount + totalTax, 2);

        var paidRatio = random.NextDouble() switch
        {
            < 0.60 => 1.0,
            < 0.80 => 0.5,
            _ => 0.0
        };
        var paid = Math.Round(total * (decimal)paidRatio, 2);

        return new PurchaseOrder
        {
            OrderNumber = $"PO-{index + 1:D6}",
            SupplierId = lookups.SupplierIds[random.Next(lookups.SupplierIds.Count)],
            WarehouseId = lookups.WarehouseIds[random.Next(lookups.WarehouseIds.Count)],
            PurchaseDate = purchaseDate,
            Status = status,
            SubTotal = subTotal,
            LabourCharge = 0m,
            TruckCharge = 0m,
            DiscountType = DiscountType.Fixed,
            DiscountAmount = totalDiscount,
            TaxAmount = totalTax,
            TotalAmount = total,
            PaidAmount = paid,
            DueAmount = Math.Round(total - paid, 2),
            ExchangeRate = 1m,
            Notes = status == PurchaseOrderStatus.Cancelled ? "Cancelled during seeding" : null,
            ExpectedDeliveryDate = purchaseDate.AddDays(random.Next(5, 45)),
            Items = items,
            TenantId = tenantId,
            CreatedBy = createdBy
        };
    }

    // ── Batched insert ────────────────────────────────────────────────────────

    /// <summary>
    /// Adds entities in batches, clearing the change tracker afterwards so memory
    /// does not grow across the tens of thousands of orders being written.
    /// </summary>
    private static async Task AddInBatchesAsync<TEntity>(
        ApplicationDbContext context, IEnumerable<TEntity> entities, int batchSize)
        where TEntity : class
    {
        var batch = new List<TEntity>(batchSize);
        foreach (var entity in entities)
        {
            batch.Add(entity);
            if (batch.Count < batchSize) continue;

            context.AddRange(batch);
            await context.SaveChangesAsync();
            batch.Clear();
            context.ChangeTracker.Clear();
        }

        if (batch.Count > 0)
        {
            context.AddRange(batch);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
        }
    }
}
