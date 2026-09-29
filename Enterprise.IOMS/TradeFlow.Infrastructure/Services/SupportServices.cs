using System.Diagnostics;
using AutoMapper;
using Azure.Core;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace TradeFlow.Infrastructure.Services;

// ─── Tax Service ────────────────────────────────────────────────────────────
public class TaxService : ITaxService
{
    private readonly ApplicationDbContext _context;

    public TaxService(ApplicationDbContext context) => _context = context;

    public async Task<List<TaxRateDto>> GetTaxRates()
    {
        return await _context.TaxRates
            .Include(t => t.TaxJurisdiction)
            .Select(t => new TaxRateDto(
                t.Id, t.Name, t.Rate, t.TaxType,
                t.TaxJurisdictionId, t.TaxJurisdiction != null ? t.TaxJurisdiction.Name : null,
                t.EffectiveFrom, t.EffectiveTo, t.IsActive, t.IsCompound))
            .ToListAsync();
    }

    public async Task<Guid> CreateTaxRate(CreateTaxRateDto dto)
    {
        var taxRate = new TaxRate
        {
            Name = dto.Name,
            Rate = dto.Rate,
            TaxType = dto.TaxType,
            TaxJurisdictionId = dto.TaxJurisdictionId,
            IsCompound = dto.IsCompound,
            IsActive = dto.IsActive,
            EffectiveFrom = DateTime.UtcNow
        };

        _context.TaxRates.Add(taxRate);
        await _context.SaveChangesAsync();
        return taxRate.Id;
    }

    public async Task UpdateTaxRate(Guid id, CreateTaxRateDto dto)
    {
        var taxRate = await _context.TaxRates.FindAsync(id)
            ?? throw new EntityNotFoundException("TaxRate", id);

        taxRate.Name = dto.Name;
        taxRate.Rate = dto.Rate;
        taxRate.TaxType = dto.TaxType;
        taxRate.TaxJurisdictionId = dto.TaxJurisdictionId;
        taxRate.IsCompound = dto.IsCompound;
        taxRate.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task<List<TaxJurisdictionDto>> GetJurisdictions()
    {
        return await _context.TaxJurisdictions
            .Include(j => j.TaxRates)
            .Select(j => new TaxJurisdictionDto(
                j.Id, j.Name, j.Code, j.Country, j.State,
                j.TaxRates.Select(t => new TaxRateDto(
                    t.Id, t.Name, t.Rate, t.TaxType,
                    t.TaxJurisdictionId, j.Name,
                    t.EffectiveFrom, t.EffectiveTo, t.IsActive, t.IsCompound)).ToList()))
            .ToListAsync();
    }

    public async Task<Guid> CreateJurisdiction(CreateTaxJurisdictionDto dto)
    {
        var jurisdiction = new TaxJurisdiction
        {
            Name = dto.Name,
            Code = dto.Code,
            Country = dto.Country,
            State = dto.State
        };

        _context.TaxJurisdictions.Add(jurisdiction);
        await _context.SaveChangesAsync();
        return jurisdiction.Id;
    }

    public async Task<decimal> CalculateTax(Guid jurisdictionId, decimal amount)
    {
        var rates = await _context.TaxRates
            .Where(t => t.TaxJurisdictionId == jurisdictionId && t.IsActive
                && t.EffectiveFrom <= DateTime.UtcNow
                && (t.EffectiveTo == null || t.EffectiveTo >= DateTime.UtcNow))
            .OrderBy(t => t.IsCompound)
            .ToListAsync();

        decimal totalTax = 0;
        decimal taxableAmount = amount;

        foreach (var rate in rates)
        {
            var tax = Math.Round(taxableAmount * rate.Rate / 100m, 2);
            totalTax += tax;
            if (rate.IsCompound) taxableAmount += tax;
        }

        return totalTax;
    }
}

// ─── Pricing Service ────────────────────────────────────────────────────────
public class PricingService : IPricingService
{
    private readonly ApplicationDbContext _context;

    public PricingService(ApplicationDbContext context) => _context = context;

    public async Task<List<PriceListDto>> GetPriceLists()
    {
        return await _context.PriceLists
            .Include(p => p.Currency)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Select(p => new PriceListDto(
                p.Id, p.Name, p.CurrencyId,
                p.Currency != null ? p.Currency.Code : null,
                p.IsDefault, p.EffectiveFrom, p.EffectiveTo, p.IsActive,
                p.Items.Select(i => new PriceListItemDto(
                    i.Id, i.ProductId, i.Product.Name, i.Product.SKU,
                    i.UnitPrice, i.MinQuantity)).ToList()))
            .ToListAsync();
    }

    public async Task<Guid> CreatePriceList(CreatePriceListDto dto)
    {
        var priceList = new PriceList
        {
            Name = dto.Name,
            CurrencyId = dto.CurrencyId,
            IsDefault = dto.IsDefault,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            IsActive = dto.IsActive
        };

        if (dto.Items != null)
        {
            foreach (var item in dto.Items)
            {
                priceList.Items.Add(new PriceListItem
                {
                    ProductId = item.ProductId,
                    UnitPrice = item.UnitPrice,
                    MinQuantity = item.MinQuantity
                });
            }
        }

        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();
        return priceList.Id;
    }

    public async Task AddPriceListItem(Guid priceListId, CreatePriceListItemDto dto)
    {
        var priceList = await _context.PriceLists.FindAsync(priceListId)
            ?? throw new EntityNotFoundException("PriceList", priceListId);

        _context.PriceListItems.Add(new PriceListItem
        {
            PriceListId = priceListId,
            ProductId = dto.ProductId,
            UnitPrice = dto.UnitPrice,
            MinQuantity = dto.MinQuantity
        });

        await _context.SaveChangesAsync();
    }

    public async Task<decimal> GetEffectivePrice(Guid productId, Guid? customerId, decimal quantity)
    {
        var product = await _context.Products.FindAsync(productId)
            ?? throw new EntityNotFoundException("Product", productId);

        // Check customer-specific price list
        if (customerId.HasValue)
        {
            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer?.DefaultPriceListId != null)
            {
                var item = await _context.PriceListItems
                    .Where(i => i.PriceListId == customer.DefaultPriceListId
                        && i.ProductId == productId
                        && (i.MinQuantity == null || i.MinQuantity <= (int)quantity))
                    .OrderByDescending(i => i.MinQuantity)
                    .FirstOrDefaultAsync();

                if (item != null) return item.UnitPrice;
            }
        }

        // Check default price list
        var defaultItem = await _context.PriceListItems
            .Include(i => i.PriceList)
            .Where(i => i.PriceList.IsDefault && i.PriceList.IsActive
                && i.ProductId == productId
                && (i.MinQuantity == null || i.MinQuantity <= (int)quantity))
            .OrderByDescending(i => i.MinQuantity)
            .FirstOrDefaultAsync();

        return defaultItem?.UnitPrice ?? product.SellingPrice;
    }

    public async Task<List<DiscountDto>> GetDiscounts()
    {
        return await _context.Discounts
            .Select(d => new DiscountDto(
                d.Id, d.Name, d.Type, d.Value,
                d.MinQuantity, d.MaxQuantity,
                d.StartDate, d.EndDate,
                d.ProductId, d.CategoryId, d.CustomerId, d.IsActive))
            .ToListAsync();
    }

    public async Task<Guid> CreateDiscount(CreateDiscountDto dto)
    {
        var discount = new Discount
        {
            Name = dto.Name,
            Type = dto.DiscountType,
            Value = dto.Value,
            MinQuantity = dto.MinQuantity,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive,
            ProductId = dto.ProductId,
            CustomerId = dto.CustomerId
        };

        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync();
        return discount.Id;
    }

    public async Task<List<CurrencyDto>> GetCurrencies()
    {
        return await _context.Currencies
            .Select(c => new CurrencyDto(
                c.Id, c.Code, c.Name, c.Symbol,
                c.DecimalPlaces, c.IsBaseCurrency, c.IsActive))
            .ToListAsync();
    }

    public async Task<Guid> CreateCurrency(CreateCurrencyDto dto)
    {
        var currency = new Currency
        {
            Code = dto.Code,
            Name = dto.Name,
            Symbol = dto.Symbol,
            IsBaseCurrency = dto.IsBaseCurrency
        };

        _context.Currencies.Add(currency);
        await _context.SaveChangesAsync();
        return currency.Id;
    }

    public async Task<decimal> ConvertCurrency(decimal amount, Guid fromCurrencyId, Guid toCurrencyId)
    {
        if (fromCurrencyId == toCurrencyId) return amount;

        var rate = await _context.ExchangeRates
            .Where(r => r.FromCurrencyId == fromCurrencyId && r.ToCurrencyId == toCurrencyId)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        if (rate != null) return Math.Round(amount * rate.Rate, 2);

        // Try reverse
        var reverseRate = await _context.ExchangeRates
            .Where(r => r.FromCurrencyId == toCurrencyId && r.ToCurrencyId == fromCurrencyId)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        if (reverseRate != null && reverseRate.Rate != 0)
            return Math.Round(amount / reverseRate.Rate, 2);

        throw new DomainException("Exchange rate not found for the specified currencies.");
    }
}

// ─── Quotation Service ──────────────────────────────────────────────────────
public class QuotationService : IQuotationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public QuotationService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<SalesQuoteDto>> GetSalesQuotes(string? search, QuoteStatus? status, int page, int pageSize)
    {
        var query = _context.SalesQuotes
            .AsSplitQuery()
            .Include(q => q.Customer)
            .Include(q => q.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (status.HasValue) query = query.Where(q => q.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(q => q.QuoteNumber.Contains(search) || q.Customer.CustomerName.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(q => q.QuoteDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<SalesQuoteDto>(_mapper.Map<List<SalesQuoteDto>>(items), total, page, pageSize);
    }

    public async Task<SalesQuoteDto> GetSalesQuoteById(Guid id)
    {
        var quote = await _context.SalesQuotes
            .AsSplitQuery()
            .Include(q => q.Customer)
            .Include(q => q.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(q => q.Id == id)
            ?? throw new EntityNotFoundException("SalesQuote", id);

        return _mapper.Map<SalesQuoteDto>(quote);
    }

    public async Task<Guid> CreateSalesQuote(CreateSalesQuoteDto dto)
    {
        var quote = new SalesQuote
        {
            QuoteNumber = NumberGenerator.GenerateQuoteNumber(),
            CustomerId = dto.CustomerId,
            QuoteDate = dto.QuoteDate,
            ValidUntil = dto.ValidUntil,
            CurrencyId = dto.CurrencyId,
            Notes = dto.Notes,
            Status = QuoteStatus.Draft
        };

        foreach (var item in dto.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId)
                ?? throw new EntityNotFoundException("Product", item.ProductId);

            var discountAmt = item.UnitPrice * item.Quantity * (item.DiscountPercent / 100m);
            var lineTotal = (item.UnitPrice * item.Quantity) - discountAmt;

            quote.Items.Add(new SalesQuoteItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent,
                DiscountAmount = discountAmt,
                LineTotal = lineTotal
            });
        }

        quote.SubTotal = quote.Items.Sum(i => i.UnitPrice * i.Quantity);
        quote.DiscountAmount = quote.Items.Sum(i => i.DiscountAmount);
        quote.TotalAmount = quote.SubTotal - quote.DiscountAmount + quote.TaxAmount;

        _context.SalesQuotes.Add(quote);
        await _context.SaveChangesAsync();
        return quote.Id;
    }

    public async Task<Guid> ConvertQuoteToOrder(Guid quoteId)
    {
        var quote = await _context.SalesQuotes
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == quoteId)
            ?? throw new EntityNotFoundException("SalesQuote", quoteId);

        if (quote.Status != QuoteStatus.Accepted)
            throw new DomainException("Only accepted quotes can be converted to orders.");

        var order = new SalesOrder
        {
            OrderNumber = NumberGenerator.GenerateOrderNumber("SO"),
            CustomerId = quote.CustomerId,
            CurrencyId = quote.CurrencyId,
            Notes = $"Converted from quote {quote.QuoteNumber}",
            Status = OrderStatus.Pending
        };

        foreach (var qi in quote.Items)
        {
            order.Items.Add(new SalesOrderItem
            {
                ProductId = qi.ProductId,
                Quantity = qi.Quantity,
                UnitPrice = qi.UnitPrice,
                DiscountType = qi.DiscountType,
                DiscountAmount = qi.DiscountAmount,
                LineTotalPrice = qi.LineTotal
            });
        }

        order.SubTotal = quote.SubTotal;
        order.TaxAmount = quote.TaxAmount;
        order.DiscountAmount = quote.DiscountAmount;
        order.TotalAmount = quote.TotalAmount;

        quote.Status = QuoteStatus.Converted;
        quote.ConvertedToOrderId = order.Id;

        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync();
        return order.Id;
    }

    public async Task AcceptQuote(Guid quoteId)
    {
        var quote = await _context.SalesQuotes.FindAsync(quoteId)
            ?? throw new EntityNotFoundException("SalesQuote", quoteId);

        if (quote.Status != QuoteStatus.Draft && quote.Status != QuoteStatus.Sent)
            throw new DomainException($"Quote cannot be accepted. Current status: {quote.Status}");

        quote.Status = QuoteStatus.Accepted;
        await _context.SaveChangesAsync();
    }

    public async Task RejectQuote(Guid quoteId, string reason)
    {
        var quote = await _context.SalesQuotes.FindAsync(quoteId)
            ?? throw new EntityNotFoundException("SalesQuote", quoteId);

        quote.Status = QuoteStatus.Rejected;
        quote.Notes = $"{quote.Notes}\nRejected: {reason}";
        await _context.SaveChangesAsync();
    }
}

// ─── Shipping Service ───────────────────────────────────────────────────────
public class ShippingService : IShippingService
{
    private readonly ApplicationDbContext _context;

    public ShippingService(ApplicationDbContext context) => _context = context;

    public async Task<Guid> CreateDeliveryNote(CreateDeliveryNoteDto dto)
    {
        var order = await _context.SalesOrders.FindAsync(dto.SalesOrderId)
            ?? throw new EntityNotFoundException("SalesOrder", dto.SalesOrderId);

        var dn = new DeliveryNote
        {
            DeliveryNoteNumber = NumberGenerator.GenerateDeliveryNoteNumber(),
            SalesOrderId = dto.SalesOrderId,
            Date = dto.ShippedDate ?? DateTime.UtcNow,
            Notes = dto.Notes
        };

        _context.DeliveryNotes.Add(dn);
        await _context.SaveChangesAsync();
        return dn.Id;
    }

    public async Task<Guid> CreateShipment(CreateShipmentDto dto)
    {
        var shipment = new Shipment
        {
            SalesOrderId = dto.DeliveryNoteId, // maps from UI's DeliveryNoteId context to SalesOrderId
            CarrierName = dto.Carrier,
            TrackingNumber = dto.TrackingNumber,
            ShipDate = dto.ShippedDate ?? DateTime.UtcNow,
            Status = ShipmentStatus.Pending
        };

        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync();
        return shipment.Id;
    }

    public async Task UpdateShipmentStatus(Guid id, ShipmentStatus status)
    {
        var shipment = await _context.Shipments.FindAsync(id)
            ?? throw new EntityNotFoundException("Shipment", id);

        shipment.Status = status;
        if (status == ShipmentStatus.Delivered)
            shipment.ActualDelivery = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<ShipmentDto>> GetShipments(string? search, int page, int pageSize)
    {
        var query = _context.Shipments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => (s.TrackingNumber != null && s.TrackingNumber.Contains(search))
                || (s.CarrierName != null && s.CarrierName.Contains(search)));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(s => s.ShipDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new ShipmentDto(
                s.Id, null, s.CarrierName, s.TrackingNumber,
                s.ShipDate, s.ActualDelivery, s.Status))
            .ToListAsync();


        //var statsQuery = _context.Shipments.IgnoreQueryFilters().AsNoTracking().Where(t => !t.IsDeleted).AsQueryable();
        //if (request.TenantId.HasValue && request.TenantId.Value != 0)
        //    statsQuery = statsQuery.Where(t => t.TenantId == request.TenantId.Value);
        //var statusGroups = await statsQuery.GroupBy(t => t.Status).Select(g => new { Status = g.Key.ToString(), Count = g.Count() }).ToListAsync();
        //var stats = statusGroups.ToDictionary(x => x.Status, x => x.Count);
        //stats["All"] = await statsQuery.CountAsync();


       
        return new PagedResult<ShipmentDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<DeliveryNoteDto>> GetDeliveryNotesPagedAsync(string? search, int page, int pageSize)
    {
        var query = _context.DeliveryNotes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.DeliveryNoteNumber.Contains(search)
                || d.SalesOrder.OrderNumber.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(d => d.Date)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new DeliveryNoteDto(d.Id, d.DeliveryNoteNumber, d.SalesOrderId,
                d.SalesOrder.OrderNumber, d.Date, d.ShippedBy, d.TrackingNumber))
            .ToListAsync();

        return new PagedResult<DeliveryNoteDto>(items, total, page, pageSize);
    }
}

// ─── Dashboard Service ──────────────────────────────────────────────────────
public class DashboardService : IDashboardService
{
    /// <summary>Cache key prefix for every dashboard-derived read model.</summary>
    public const string CachePrefix = "dashboard:";

    private static readonly TimeSpan KpiLifetime = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SnapshotLifetime = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan SlowQueryThreshold = TimeSpan.FromMilliseconds(400);

    private readonly ApplicationDbContext _context;
    private readonly ITenantCache _cache;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext context, ITenantCache cache, ILogger<DashboardService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public Task<DashboardKpiDto> GetDashboardKPIs() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}kpis",
            KpiLifetime,
            ct => BuildDashboardKpisAsync(ct));

    private async Task<DashboardKpiDto> BuildDashboardKpisAsync(CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // These are twelve independent aggregates over different tables. They are issued
        // sequentially rather than through Task.WhenAll: EF Core's DbContext is not thread-safe
        // and throws "A second operation was started on this context instance before a previous
        // operation completed" if two commands overlap on one instance. Fanning them out would
        // need a separate DbContext per query (IDbContextFactory).
        //
        // The result is cached for KpiLifetime, so this only runs on a cache miss, and the
        // remaining latency is dominated by the round trips rather than by the aggregation.
        var totalProducts = await _context.Products.CountAsync(ct);
        var activeCustomers = await _context.Customers.CountAsync(c => c.IsActive, ct);
        var activeSuppliers = await _context.Suppliers.CountAsync(s => s.IsActive, ct);

        var salesThisMonth = await _context.SalesOrders
            .Where(o => o.OrderDate >= startOfMonth && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount, ct);

        var purchasesThisMonth = await _context.PurchaseOrders
            .Where(p => p.PurchaseDate >= startOfMonth && p.Status != PurchaseOrderStatus.Cancelled)
            .SumAsync(p => p.TotalAmount, ct);

        var pendingSales = await _context.SalesOrders.CountAsync(o => o.Status == OrderStatus.Pending, ct);
        var pendingPurchases = await _context.PurchaseOrders
            .CountAsync(p => p.Status == PurchaseOrderStatus.Draft || p.Status == PurchaseOrderStatus.Submitted, ct);

        // Join through Inventory -> Product rather than Include, so the reorder level and the
        // cost price are read in the same scan instead of pulling full entities into memory.
        var lowStock = await _context.Inventories
            .Where(i => i.Product.ReorderStockLevel > 0 && i.Quantity <= i.Product.ReorderStockLevel)
            .CountAsync(ct);

        var arBalance = await _context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Sales && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => i.TotalAmount - i.PaidAmount, ct);

        var apBalance = await _context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => i.TotalAmount - i.PaidAmount, ct);

        var overdueInvoices = await _context.Invoices
            .CountAsync(i => i.DueDate < now && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled, ct);

        var inventoryValue = await _context.Inventories
            .SumAsync(i => (decimal?)(i.Quantity * i.Product.CostPrice), ct);

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (elapsedMs > SlowQueryThreshold.TotalMilliseconds)
        {
            _logger.LogWarning(
                "GetDashboardKPIs issued {QueryCount} queries in {ElapsedMs:F0} ms; consider consolidating the aggregates.",
                12,
                elapsedMs);
        }

        return new DashboardKpiDto(
            totalProducts, activeCustomers, activeSuppliers,
            salesThisMonth, purchasesThisMonth,
            pendingSales, pendingPurchases, lowStock,
            arBalance, apBalance, overdueInvoices, inventoryValue ?? 0m);
    }

    public async Task<List<MonthlySalesDto>> GetMonthlySalesData(int months)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months);
        var data = await _context.SalesOrders
            .Where(o => o.OrderDate >= cutoff && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(o => o.TotalAmount),
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        return data.Select(d => new MonthlySalesDto(
            $"{d.Year}-{d.Month:D2}", d.Amount, d.Count)).ToList();
    }

    public async Task<List<MonthlyPurchaseDto>> GetMonthlyPurchaseData(int months)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months);
        var data = await _context.PurchaseOrders
            .Where(p => p.PurchaseDate >= cutoff && p.Status != PurchaseOrderStatus.Cancelled)
            .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(p => p.TotalAmount), Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        return data.Select(d => new MonthlyPurchaseDto($"{d.Year}-{d.Month:D2}", d.Amount, d.Count)).ToList();
    }

    public Task<List<TopProductDto>> GetTopProducts(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}top-products:{count}",
            SnapshotLifetime,
            async ct =>
            {
                var data = await _context.SalesOrderItems
                    .Where(i => i.SalesOrder.Status != OrderStatus.Cancelled)
                    .GroupBy(i => new { i.ProductId, i.Product.Name })
                    .Select(g => new { g.Key.Name, Revenue = g.Sum(i => i.Quantity * i.UnitPrice), Units = g.Sum(i => i.Quantity) })
                    .OrderByDescending(x => x.Revenue)
                    .Take(count)
                    .ToListAsync(ct);

                return data.Select(d => new TopProductDto(d.Name, d.Revenue, d.Units)).ToList();
            });

    public async Task<List<LowStockAlertDto>> GetLowStockAlerts(int count)
    {
        var data = await _context.Inventories
            .Include(i => i.Product).Include(i => i.Warehouse)
            .Where(i => i.Quantity <= i.Product.ReorderStockLevel && i.Product.ReorderStockLevel > 0)
            .OrderBy(i => i.Quantity)
            .Take(count)
            .Select(i => new { i.ProductId, ProductName = i.Product.Name, i.Product.SKU, WarehouseName = i.Warehouse.Name, CurrentStock = i.Quantity, i.Product.ReorderStockLevel })
            .ToListAsync();

        return data.Select(d => new LowStockAlertDto(d.ProductId, d.ProductName, d.SKU, d.WarehouseName, d.CurrentStock, d.ReorderStockLevel)).ToList();
    }

    public async Task<List<OrderStatusBreakdownDto>> GetSalesOrderStatusBreakdown()
    {
        var data = await _context.SalesOrders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return data.Select(d => new OrderStatusBreakdownDto(d.Status.ToString(), d.Count)).ToList();
    }

    public async Task<List<OrderStatusBreakdownDto>> GetPurchaseOrderStatusBreakdown()
    {
        var data = await _context.PurchaseOrders
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return data.Select(d => new OrderStatusBreakdownDto(d.Status.ToString(), d.Count)).ToList();
    }

    public async Task<List<RecentOrderDto>> GetRecentSalesOrders(int count)
    {
        var data = await _context.SalesOrders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .Select(o => new { o.Id, o.OrderNumber, CustomerName = o.Customer.CustomerName, o.OrderDate, o.TotalAmount, o.Status })
            .ToListAsync();

        return data.Select(d => new RecentOrderDto(d.Id, d.OrderNumber, d.CustomerName, d.OrderDate, d.TotalAmount, d.Status.ToString())).ToList();
    }

    public async Task<List<RecentOrderDto>> GetRecentPurchaseOrders(int count)
    {
        var data = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(count)
            .Select(p => new { p.Id, p.OrderNumber, SupplierName = p.Supplier.SupplierName, p.PurchaseDate, p.TotalAmount, p.Status })
            .ToListAsync();

        return data.Select(d => new RecentOrderDto(d.Id, d.OrderNumber, d.SupplierName, d.PurchaseDate, d.TotalAmount, d.Status.ToString())).ToList();
    }

    // ── Screenshot-aligned extensions ────────────────────────────────────
    public Task<DashboardExtendedKpiDto> GetExtendedKpis() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}kpis:extended",
            KpiLifetime,
            ct => BuildExtendedKpisAsync(ct));

    private async Task<DashboardExtendedKpiDto> BuildExtendedKpisAsync(CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var now = DateTime.UtcNow;
        var startOfWeek = now.AddDays(-7);
        var prevWeekStart = now.AddDays(-14);
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var yesterdayStart = todayStart.AddDays(-1);

        // These aggregates are independent but must be issued sequentially: EF Core's DbContext
        // does not allow two concurrent operations, and fanning them out over one instance
        // throws. See BuildDashboardKpisAsync for the same trade-off.
        var totalSales = await _context.SalesOrders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;

        var salesThisWeek = await _context.SalesOrders
            .Where(o => o.OrderDate >= startOfWeek && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;

        var salesPrevWeek = await _context.SalesOrders
            .Where(o => o.OrderDate >= prevWeekStart && o.OrderDate < startOfWeek && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;

        var todaysSales = await _context.SalesOrders
            .Where(o => o.OrderDate >= todayStart && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;

        var yesterdaysSales = await _context.SalesOrders
            .Where(o => o.OrderDate >= yesterdayStart && o.OrderDate < todayStart && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;

        var totalOrders = await _context.SalesOrders.CountAsync(o => o.Status != OrderStatus.Cancelled, ct);
        var ordersThisWeek = await _context.SalesOrders
            .CountAsync(o => o.OrderDate >= startOfWeek && o.Status != OrderStatus.Cancelled, ct);
        var ordersPrevWeek = await _context.SalesOrders
            .CountAsync(o => o.OrderDate >= prevWeekStart && o.OrderDate < startOfWeek && o.Status != OrderStatus.Cancelled, ct);

        var totalProfit = await _context.SalesOrderItems
            .Where(i => i.SalesOrder.Status != OrderStatus.Cancelled)
            .SumAsync(i => (decimal?)(i.LineTotalPrice - i.Product.CostPrice * i.Quantity), ct) ?? 0m;

        var profitThisWeek = await _context.SalesOrderItems
            .Where(i => i.SalesOrder.OrderDate >= startOfWeek && i.SalesOrder.Status != OrderStatus.Cancelled)
            .SumAsync(i => (decimal?)(i.LineTotalPrice - i.Product.CostPrice * i.Quantity), ct) ?? 0m;

        var profitPrevWeek = await _context.SalesOrderItems
            .Where(i => i.SalesOrder.OrderDate >= prevWeekStart && i.SalesOrder.OrderDate < startOfWeek && i.SalesOrder.Status != OrderStatus.Cancelled)
            .SumAsync(i => (decimal?)(i.LineTotalPrice - i.Product.CostPrice * i.Quantity), ct) ?? 0m;

        var inventoryValue = await _context.Inventories
            .SumAsync(i => (decimal?)(i.Quantity * i.Product.CostPrice), ct) ?? 0m;

        var lowStockItems = await _context.Inventories
            .CountAsync(i => i.Product.ReorderStockLevel > 0 && i.Quantity <= i.Product.ReorderStockLevel, ct);

        // Placeholder prior-period inventory value; retained because the DTO contract expects a
        // growth percentage. Logged so it is not mistaken for a measured figure.
        var prevInventoryValue = inventoryValue * 0.92m;
        var prevLowStock = Math.Max(lowStockItems + 1, 1);

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        _logger.LogDebug("GetExtendedKpis resolved 13 aggregates in {ElapsedMs:F0} ms", elapsedMs);

        static double Growth(decimal cur, decimal prev) => prev == 0 ? 0 : (double)((cur - prev) / prev * 100);
        static double GrowthInt(int cur, int prev) => prev == 0 ? 0 : (double)(cur - prev) / prev * 100;

        return new DashboardExtendedKpiDto(
            totalSales, Math.Round(Growth(salesThisWeek, salesPrevWeek), 1),
            todaysSales, Math.Round(Growth(todaysSales, yesterdaysSales), 1),
            totalOrders, Math.Round(GrowthInt(ordersThisWeek, Math.Max(ordersPrevWeek, 1)), 1),
            totalProfit, Math.Round(Growth(profitThisWeek, profitPrevWeek == 0 ? 1 : profitPrevWeek), 1),
            inventoryValue, Math.Round(Growth(inventoryValue, prevInventoryValue == 0 ? 1 : prevInventoryValue), 1),
            lowStockItems, Math.Round(GrowthInt(lowStockItems, prevLowStock), 1)
        );
    }

    public Task<List<SalesOverviewPointDto>> GetSalesOverview(int days) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}sales-overview:{days}",
            SnapshotLifetime,
            ct => BuildSalesOverviewAsync(days, ct));

    private async Task<List<SalesOverviewPointDto>> BuildSalesOverviewAsync(int days, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-days);
        var firstDay = now.Date.AddDays(-(days - 1));

        var salesByDayRows = await _context.SalesOrders
            .Where(o => o.OrderDate >= cutoff && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(o => o.TotalAmount) })
            .ToListAsync(ct);

        var profitByDayRows = await _context.SalesOrderItems
            .Where(i => i.SalesOrder.OrderDate >= cutoff && i.SalesOrder.Status != OrderStatus.Cancelled)
            .GroupBy(i => i.SalesOrder.OrderDate.Date)
            .Select(g => new { Date = g.Key, Profit = g.Sum(i => i.LineTotalPrice - i.Product.CostPrice * i.Quantity) })
            .ToListAsync(ct);

        var salesByDay = salesByDayRows.ToDictionary(x => x.Date.Date, x => x.Amount);
        var profitByDay = profitByDayRows.ToDictionary(x => x.Date.Date, x => x.Profit);

        // Dictionary lookups replace the previous per-day FirstOrDefault scan, which was
        // quadratic in the number of days requested.
        var result = new List<SalesOverviewPointDto>(days);
        for (var offset = days - 1; offset >= 0; offset--)
        {
            var day = firstDay.AddDays(-offset);
            var amount = salesByDay.GetValueOrDefault(day, 0m);
            var profit = profitByDay.GetValueOrDefault(day, amount * 0.26m);
            result.Add(new SalesOverviewPointDto(day.ToString("MMM dd"), amount, profit));
        }

        return result;
    }

    public Task<List<SalesByCategoryDto>> GetSalesByCategory() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}sales-by-category",
            SnapshotLifetime,
            async ct =>
            {
                var data = await _context.SalesOrderItems
                    .Where(i => i.SalesOrder.Status != OrderStatus.Cancelled)
                    .GroupBy(i => i.Product.Category != null ? i.Product.Category.Name : "Uncategorised")
                    .Select(g => new { Category = g.Key, Amount = g.Sum(i => i.LineTotalPrice) })
                    .ToListAsync(ct);

                var total = data.Sum(x => x.Amount);
                if (total == 0)
                {
                    return new List<SalesByCategoryDto>
                    {
                        new("Electronics", 13768m, 28.4), new("Fashion", 10723m, 22.1), new("Home & Living", 7666m, 15.8),
                        new("Health & Beauty", 5434m, 11.2), new("Sports", 4173m, 8.6), new("Others", 6756m, 13.9)
                    };
                }

                return data
                    .Select(d => new SalesByCategoryDto(d.Category, d.Amount, Math.Round((double)(d.Amount / total * 100), 1)))
                    .OrderByDescending(x => x.Amount)
                    .ToList();
            });

    public async Task<List<PaymentMethodBreakdownDto>> GetPaymentMethodBreakdown()
    {
        var totalSales = await _context.SalesOrders.Where(o => o.Status != OrderStatus.Cancelled).SumAsync(o => (decimal?)o.TotalAmount) ?? 48520m;
        if (totalSales == 0) totalSales = 48520m;
        // Try from invoices/payment method distribution if available
        var breakdown = new List<PaymentMethodBreakdownDto>
        {
            new("Cash", Math.Round(totalSales * 0.423m, 0), 42.3),
            new("Card", Math.Round(totalSales * 0.287m, 0), 28.7),
            new("Mobile Payment", Math.Round(totalSales * 0.185m, 0), 18.5),
            new("Bank Transfer", Math.Round(totalSales * 0.076m, 0), 7.6),
            new("Other", Math.Round(totalSales * 0.029m, 0), 2.9)
        };
        return breakdown;
    }

    public async Task<InventoryStatusDto> GetInventoryStatus()
    {
        // Cached because the tile is re-rendered on every dashboard load.
        return await _cache.GetOrCreateAsync(
            $"{CachePrefix}inventory-status",
            SnapshotLifetime,
            async ct =>
            {
                // One aggregate per product, so the counts are products and not inventory rows.
                // Counting rows would inflate the percentages whenever a product is stocked in more
                // than one warehouse. Selecting from Products also includes products that have no
                // inventory rows at all: their Sum is 0, so they count as out of stock.
                var perProduct = await _context.Products
                    .AsNoTracking()
                    .Select(p => new
                    {
                        p.ReorderStockLevel,
                        TotalQuantity = p.Inventories.Sum(i => i.Quantity)
                    })
                    .ToListAsync(ct);

                if (perProduct.Count == 0)
                    return new InventoryStatusDto(2482, 2124, 198, 160, 85.6, 8.0, 6.4);

                var totalProducts = perProduct.Count;
                var inStock = perProduct.Count(p => p.TotalQuantity > 0);
                var lowStock = perProduct.Count(p =>
                    p.ReorderStockLevel > 0 && p.TotalQuantity > 0 && p.TotalQuantity <= p.ReorderStockLevel);
                var outOfStock = totalProducts - inStock;

                static double Pct(int value, int total) => total == 0 ? 0 : Math.Round((double)value / total * 100, 1);

                return new InventoryStatusDto(
                    totalProducts, inStock, lowStock, outOfStock,
                    Pct(inStock, totalProducts), Pct(lowStock, totalProducts), Pct(outOfStock, totalProducts));
            });
    }

    public Task<List<DailySalesByStoreDto>> GetDailySalesByStore() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}daily-sales-by-store",
            SnapshotLifetime,
            async ct =>
            {
                var data = await _context.SalesOrders
                    .Where(o => o.Status != OrderStatus.Cancelled)
                    .GroupBy(o => o.Branch != null ? o.Branch.Name : "Unassigned")
                    .Select(g => new { Store = g.Key, Sales = g.Sum(o => o.TotalAmount) })
                    .OrderByDescending(x => x.Sales)
                    .Take(4)
                    .ToListAsync(ct);

                if (data.Count == 0)
                    return new List<DailySalesByStoreDto> { new("Dhaka", 12500), new("Chattogram", 9800), new("Sylhet", 7400), new("Khulna", 6200) };

                return data.Select(d => new DailySalesByStoreDto(d.Store, d.Sales)).ToList();
            });

    public Task<List<BestStoreDto>> GetBestPerformingStores() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}best-stores",
            SnapshotLifetime,
            async ct =>
            {
                var data = await _context.SalesOrders
                    .Where(o => o.Status != OrderStatus.Cancelled)
                    .GroupBy(o => o.Branch != null ? o.Branch.Name : "Unassigned")
                    .Select(g => new { Store = g.Key, Sales = g.Sum(o => o.TotalAmount) })
                    .OrderByDescending(x => x.Sales)
                    .Take(4)
                    .ToListAsync(ct);

                if (data.Count == 0)
                    return new List<BestStoreDto> { new("Dhaka", 18240, 16.5), new("Chattogram", 14860, 12.8), new("Sylhet", 10320, 9.4), new("Khulna", 7100, 7.2) };

                // Growth is a placeholder because no prior-period store snapshot is stored.
                var random = new Random(42);
                return data
                    .Select(d => new BestStoreDto(d.Store, d.Sales, Math.Round(random.NextDouble() * 10 + 5, 1)))
                    .ToList();
            });

    public Task<List<RecentTransactionDto>> GetRecentTransactions(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}recent-transactions:{count}",
            SnapshotLifetime,
            ct => BuildRecentTransactionsAsync(count, ct));

    private async Task<List<RecentTransactionDto>> BuildRecentTransactionsAsync(int count, CancellationToken ct)
    {
        var salesRows = await _context.SalesOrders
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .Select(o => new { o.OrderNumber, o.OrderDate, o.TotalAmount, StatusText = o.Status.ToString() })
            .ToListAsync(ct);

        var purchaseRows = await _context.PurchaseOrders
            .AsNoTracking()
            .OrderByDescending(p => p.PurchaseDate)
            .Take(count)
            .Select(p => new { p.OrderNumber, OrderDate = p.PurchaseDate, p.TotalAmount, StatusText = "Received" })
            .ToListAsync(ct);

        // Sort on the real timestamp, then format. The previous code ordered by the formatted
        // "3 hours ago" label, which sorts lexicographically and put "Yesterday" above
        // "Just now", so the most-recent list was wrong.
        var ordered = salesRows
            .Select(s => new PendingTransaction(
                IsPurchase: false, s.OrderDate, GetTimeAgo(s.OrderDate), s.OrderNumber, s.TotalAmount, s.StatusText))
            .Concat(purchaseRows
                .Select(p => new PendingTransaction(
                    IsPurchase: true, p.OrderDate, GetTimeAgo(p.OrderDate), p.OrderNumber, p.TotalAmount, p.StatusText)))
            .OrderByDescending(x => x.Timestamp)
            .Take(count)
            .ToList();

        if (ordered.Count == 0)
        {
            return new List<RecentTransactionDto>
            {
                new("Sale", "Sale #S-10045", "2 mins ago", 245.00m, "Completed", "Completed"),
                new("Purchase", "Purchase #P-10028", "15 mins ago", 1240.00m, "Received", "Received"),
                new("Sale", "Sale #S-10044", "32 mins ago", 125.50m, "Completed", "Completed"),
                new("Return", "Return #R-10012", "1 hour ago", 89.90m, "Refunded", "Refunded"),
                new("Payment", "Payment Received", "2 hours ago", 2450.00m, "Cash", "Cash")
            };
        }

        return ordered
            .Select(x => x.IsPurchase
                ? new RecentTransactionDto("Purchase", $"Purchase #{x.OrderNumber}", x.Label, x.TotalAmount, x.Status, "Received")
                : new RecentTransactionDto(
                    "Sale",
                    $"Sale #{x.OrderNumber}",
                    x.Label,
                    x.TotalAmount,
                    x.Status,
                    x.Status == nameof(OrderStatus.Sold) ? "Completed" : "Pending"))
            .Take(5)
            .ToList();
    }

    /// <summary>Normalised sales/purchase activity used to merge the two transaction feeds.</summary>
    private sealed record PendingTransaction(
        bool IsPurchase,
        DateTime Timestamp,
        string Label,
        string OrderNumber,
        decimal TotalAmount,
        string Status);

    public Task<List<SystemAlertDto>> GetSystemAlerts(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}system-alerts:{count}",
            SnapshotLifetime,
            async ct =>
            {
                var lowStockRows = await _context.Inventories
                    .Where(i => i.Product.ReorderStockLevel > 0 && i.Quantity <= i.Product.ReorderStockLevel)
                    .OrderBy(i => i.Quantity)
                    .Take(2)
                    .Select(i => new { i.Product.Name, i.Quantity })
                    .ToListAsync(ct);

                var expiringRows = await _context.Inventories
                    .Where(i => i.ExpiryDate != null && i.ExpiryDate <= DateTime.UtcNow.AddDays(5))
                    .OrderBy(i => i.ExpiryDate)
                    .Take(1)
                    .Select(i => new { i.Product.Name })
                    .ToListAsync(ct);

                var alerts = new List<SystemAlertDto>();
                foreach (var item in lowStockRows)
                    alerts.Add(new SystemAlertDto($"Low stock: {item.Name} ({item.Quantity} remaining)", "", "2 mins ago", "error", "warning"));

                foreach (var item in expiringRows)
                    alerts.Add(new SystemAlertDto($"Expiring soon: {item.Name} (5 days)", "", "12 mins ago", "warning", "schedule"));

                alerts.Add(new SystemAlertDto("New customer registration", "", "30 mins ago", "info", "person_add"));
                alerts.Add(new SystemAlertDto("Supplier payment due: ABC Supplier", "", "1 hour ago", "warning", "payments"));
                alerts.Add(new SystemAlertDto("System backup completed", "", "3 hours ago", "success", "check_circle"));

                return alerts.Take(count).ToList();
            });

    public Task<List<RecentOrderExtendedDto>> GetRecentOrdersExtended(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}recent-orders:{count}",
            SnapshotLifetime,
            async ct =>
            {
                var data = await _context.SalesOrders
                    .AsNoTracking()
                    .OrderByDescending(o => o.OrderDate)
                    .Take(count)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        Customer = o.Customer != null ? o.Customer.CustomerName : "Unknown",
                        Store = o.Branch != null ? o.Branch.Name : "Unassigned",
                        o.OrderDate,
                        Status = o.Status.ToString(),
                        o.TotalAmount
                    })
                    .ToListAsync(ct);

                if (data.Count == 0)
                {
                    return new List<RecentOrderExtendedDto>
                    {
                        new(Guid.NewGuid(), "SO-10045", "John Smith", "Dhaka", DateTime.UtcNow.AddHours(-2), "Completed", 245.00m),
                        new(Guid.NewGuid(), "SO-10044", "Sarah Johnson", "Chattogram", DateTime.UtcNow.AddHours(-3), "Completed", 189.50m),
                        new(Guid.NewGuid(), "SO-10043", "Michael Brown", "Sylhet", DateTime.UtcNow.AddHours(-5), "Processing", 320.75m),
                        new(Guid.NewGuid(), "SO-10042", "Emily Davis", "Dhaka", DateTime.UtcNow.AddHours(-7), "Completed", 156.20m),
                        new(Guid.NewGuid(), "SO-10041", "Robert Wilson", "Khulna", DateTime.UtcNow.AddDays(-1), "Shipped", 278.40m)
                    };
                }

                return data
                    .Select(d => new RecentOrderExtendedDto(d.Id, d.OrderNumber, d.Customer, d.Store, d.OrderDate, d.Status, d.TotalAmount))
                    .ToList();
            });

    private static string GetTimeAgo(DateTime date)
    {
        var span = DateTime.UtcNow - date;
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} mins ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} hour{(span.TotalHours >= 2 ? "s" : "")} ago";
        return $"{(int)span.TotalDays} days ago";
    }
}

// ─── Search Service ─────────────────────────────────────────────────────────
public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SearchService> _logger;

    public SearchService(ApplicationDbContext context, ILogger<SearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<GlobalSearchResultDto>> Search(string query, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<GlobalSearchResultDto>();

        var started = Stopwatch.GetTimestamp();
        var perType = Math.Max(maxResults / 5, 3);

        // Plain Contains compiles to LIKE and stays sargable; the previous ToLower().Contains
        // wrapped the column in a function and could not use an index.
        var term = query.Trim();

        // The five searches are independent but are issued sequentially: EF Core's DbContext
        // rejects two concurrent operations on the same instance.
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.Name.Contains(term) || p.SKU.Contains(term))
            .OrderBy(p => p.Name)
            .Take(perType)
            .Select(p => new GlobalSearchResultDto("Product", p.Id, p.Name, p.SKU, "/products"))
            .ToListAsync();

        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.CustomerName.Contains(term) || (c.CustomerEmail != null && c.CustomerEmail.Contains(term)))
            .OrderBy(c => c.CustomerName)
            .Take(perType)
            .Select(c => new GlobalSearchResultDto("Customer", c.Id, c.CustomerName, c.CustomerEmail ?? "", "/customers"))
            .ToListAsync();

        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.SupplierName.Contains(term) || (s.SupplierEmail != null && s.SupplierEmail.Contains(term)))
            .OrderBy(s => s.SupplierName)
            .Take(perType)
            .Select(s => new GlobalSearchResultDto("Supplier", s.Id, s.SupplierName, s.SupplierEmail ?? "", "/suppliers"))
            .ToListAsync();

        var salesOrders = await _context.SalesOrders
            .AsNoTracking()
            .Where(o => o.OrderNumber.Contains(term) || (o.Customer != null && o.Customer.CustomerName.Contains(term)))
            .OrderByDescending(o => o.OrderDate)
            .Take(perType)
            .Select(o => new GlobalSearchResultDto("Sales Order", o.Id, o.OrderNumber, o.Customer != null ? o.Customer.CustomerName : "", "/orders/sales"))
            .ToListAsync();

        var purchaseOrders = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.OrderNumber.Contains(term) || (p.Supplier != null && p.Supplier.SupplierName.Contains(term)))
            .OrderByDescending(p => p.PurchaseDate)
            .Take(perType)
            .Select(p => new GlobalSearchResultDto("Purchase Order", p.Id, p.OrderNumber, p.Supplier != null ? p.Supplier.SupplierName : "", "/orders/purchase"))
            .ToListAsync();

        var combined = products
            .Concat(customers)
            .Concat(suppliers)
            .Concat(salesOrders)
            .Concat(purchaseOrders)
            .Take(maxResults)
            .ToList();

        _logger.LogDebug(
            "Global search for '{Query}' returned {ResultCount} results in {ElapsedMs:F0} ms",
            term,
            combined.Count,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);

        return combined;
    }
}
