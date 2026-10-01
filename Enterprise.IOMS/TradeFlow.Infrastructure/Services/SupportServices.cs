using System.Diagnostics;
using System.Text;
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
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public QuotationService(ApplicationDbContext context, IMapper mapper, IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _context = context;
        _mapper = mapper;
        _contextFactory = contextFactory;
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

    /// <summary>
    /// Server-side paged quotation list used by the Quotations grid. Mirrors
    /// OrderService.GetSalesOrdersAsync: zero-based paging, filterable, sortable,
    /// and returns per-status counts in <see cref="PagedResultNew{T}.Stats"/>.
    /// </summary>
    public async Task<PagedResultNew<SalesQuoteDto>> GetSalesQuotesAsync(QuotationPagedRequest request)
    {
        // Own context for the whole read: the list can be re-entered while another query on the
        // scoped context is still in flight, and a DbContext cannot run two commands at once.
        await using var read = await _contextFactory.CreateDbContextAsync();
        var response = new PagedResultNew<SalesQuoteDto>();

        var page = Math.Max(0, request.CurrentPage);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var search = request.SearchTerm?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        IQueryable<SalesQuote> query;
        if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
            query = read.SalesQuotes.IgnoreQueryFilters().AsNoTracking()
                .Where(q => !q.IsDeleted && q.TenantId == request.TenantId.Value);
        else
            query = read.SalesQuotes.AsNoTracking().Where(q => !q.IsDeleted);

        // Stats are computed over the non-status filters so the tiles keep showing
        // every bucket while a single status tile is active.
        var statsSource = ApplyQuotationFilters(query, request, search, hasSearch);

        var groups = await statsSource
            .GroupBy(q => q.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byStatus = groups.ToDictionary(g => g.Status, g => g.Count);
        response.Stats["TotalCount"] = byStatus.Values.Sum();
        foreach (var s in Enum.GetValues<QuoteStatus>())
            response.Stats[$"{s}Count"] = byStatus.TryGetValue(s, out var c) ? c : 0;
        response.Stats["TotalValue"] = (int)Math.Round(
            await statsSource.SumAsync(q => (decimal?)q.TotalAmount) ?? 0m);

        if (request.Status.HasValue)
        {
            var status = request.Status.Value;
            query = query.Where(q => q.Status == status);
        }

        query = ApplyQuotationFilters(query, request, search, hasSearch);

        var sortAsc = request.SortAscending;
        query = (request.SortColumn ?? "QuoteDate") switch
        {
            "QuoteNumber" => sortAsc ? query.OrderBy(q => q.QuoteNumber) : query.OrderByDescending(q => q.QuoteNumber),
            "Customer" => sortAsc
                ? query.OrderBy(q => q.Customer.CustomerName)
                : query.OrderByDescending(q => q.Customer.CustomerName),
            "ValidUntil" => sortAsc ? query.OrderBy(q => q.ValidUntil) : query.OrderByDescending(q => q.ValidUntil),
            "TotalAmount" => sortAsc ? query.OrderBy(q => q.TotalAmount) : query.OrderByDescending(q => q.TotalAmount),
            "Status" => sortAsc ? query.OrderBy(q => q.Status) : query.OrderByDescending(q => q.Status),
            _ => sortAsc ? query.OrderBy(q => q.QuoteDate) : query.OrderByDescending(q => q.QuoteDate),
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .AsSplitQuery()
            .Include(q => q.Customer)
            .Include(q => q.Items).ThenInclude(i => i.Product)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        response.Items = _mapper.Map<List<SalesQuoteDto>>(items);
        response.TotalCount = totalCount;
        response.CurrentPage = page;
        response.PageSize = pageSize;

        return response;
    }

    private static IQueryable<SalesQuote> ApplyQuotationFilters(
        IQueryable<SalesQuote> query, QuotationPagedRequest request, string? search, bool hasSearch)
    {
        if (hasSearch)
        {
            query = query.Where(q =>
                EF.Functions.Like(q.QuoteNumber, $"%{search}%") ||
                EF.Functions.Like(q.Customer.CustomerName, $"%{search}%") ||
                EF.Functions.Like(q.Notes, $"%{search}%"));
        }

        if (request.CustomerId.HasValue && request.CustomerId != Guid.Empty)
            query = query.Where(q => q.CustomerId == request.CustomerId.Value);

        if (request.From.HasValue)
            query = query.Where(q => q.QuoteDate >= request.From.Value.Date);

        if (request.To.HasValue)
        {
            var toExclusive = request.To.Value.Date.AddDays(1);
            query = query.Where(q => q.QuoteDate < toExclusive);
        }

        return query;
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

    /// <summary>
    /// Longest overview window any dashboard range preset can ask for. A "Last 12 Months" window
    /// reaches 366 days when it spans a leap day, so the bound has to allow for that.
    /// </summary>
    private const int MaxOverviewDays = 366;

    /// <summary>
    /// Windows wider than this are plotted as monthly buckets instead of daily ones. At daily
    /// granularity a one-year window is 365 near-identical samples that render as a solid block with
    /// an x-axis whose "Oct 02" and "Oct 01" ends cannot be told apart; monthly buckets show the same
    /// trend in a readable number of points. Totals are unaffected — only the grouping changes.
    /// </summary>
    private const int MonthlyBucketThresholdDays = 62;

    private readonly ApplicationDbContext _context;
    private readonly ITenantCache _cache;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApplicationDbContext context,
        ITenantCache cache,
        ITenantProvider tenantProvider,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _cache = cache;
        _tenantProvider = tenantProvider;
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

        // Inventory value and low-stock count are point-in-time balances: there is no stored
        // snapshot of either, so no honest period-over-period comparison exists. The previous
        // implementation invented one (inventoryValue * 0.92 and lowStockItems + 1) and rendered
        // the result as a measured trend. Both growth figures are now left null, which makes the
        // tile omit its trend arrow instead of asserting a number nobody computed.
        double? prevInventoryValue = null;
        double? prevLowStock = null;

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        _logger.LogDebug("GetExtendedKpis resolved 13 aggregates in {ElapsedMs:F0} ms", elapsedMs);

        static double Growth(decimal cur, decimal prev) => prev == 0 ? 0 : (double)((cur - prev) / prev * 100);
        static double GrowthInt(int cur, int prev) => prev == 0 ? 0 : (double)(cur - prev) / prev * 100;

        return new DashboardExtendedKpiDto(
            totalSales, Math.Round(Growth(salesThisWeek, salesPrevWeek), 1),
            todaysSales, Math.Round(Growth(todaysSales, yesterdaysSales), 1),
            totalOrders, Math.Round(GrowthInt(ordersThisWeek, Math.Max(ordersPrevWeek, 1)), 1),
            totalProfit, Math.Round(Growth(profitThisWeek, profitPrevWeek == 0 ? 1 : profitPrevWeek), 1),
            inventoryValue,
            prevInventoryValue.HasValue ? Math.Round(Growth(inventoryValue, (decimal)prevInventoryValue.Value), 1) : null,
            lowStockItems,
            prevLowStock.HasValue ? Math.Round(GrowthInt(lowStockItems, (int)prevLowStock.Value), 1) : null
        );
    }

    public Task<List<SalesOverviewPointDto>> GetSalesOverview(int days) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}sales-overview:{days}",
            SnapshotLifetime,
            ct => BuildSalesOverviewAsync(days, ct));

    private async Task<List<SalesOverviewPointDto>> BuildSalesOverviewAsync(int days, CancellationToken ct)
    {
        days = Math.Clamp(days, 1, MaxOverviewDays);

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

        // Months with no orders at all are still emitted, so a gap in activity reads as a zero
        // rather than silently shortening the strip.
        if (days > MonthlyBucketThresholdDays)
        {
            // The final month is cut off at today. A future-dated order in the current month would
            // otherwise be counted in the bucket while the daily view, which stops at today, drops it.
            var endOfToday = now.Date.AddDays(1);
            var monthly = new List<SalesOverviewPointDto>();
            for (var month = new DateTime(firstDay.Year, firstDay.Month, 1); month <= now; month = month.AddMonths(1))
            {
                var next = month.AddMonths(1);
                if (next > endOfToday) next = endOfToday;

                var amount = SumBetween(salesByDay, month, next);
                var profit = SumBetween(profitByDay, month, next);
                monthly.Add(new SalesOverviewPointDto(month.ToString("MMM yyyy"), amount, profit));
            }
            return monthly;
        }

        // Dictionary lookups replace the previous per-day FirstOrDefault scan, which was
        // quadratic in the number of days requested.
        var result = new List<SalesOverviewPointDto>(days);
        for (var offset = days - 1; offset >= 0; offset--)
        {
            var day = firstDay.AddDays(-offset);
            var amount = salesByDay.GetValueOrDefault(day, 0m);

            // Profit is only known for days that actually contain order lines. A day with orders but
            // no matching line rows yields 0, which is the real figure; the previous code substituted
            // amount * 0.26, inventing a flat 26% margin for every day whose profit could not be
            // measured.
            var profit = profitByDay.GetValueOrDefault(day, 0m);
            result.Add(new SalesOverviewPointDto(day.ToString("MMM dd"), amount, profit));
        }

        return result;
    }

    /// <summary>
    /// Sums a per-day series over a half-open month window. Done in memory rather than in the query
    /// so the day grouping stays a plain GroupBy on a column EF can translate.
    /// </summary>
    private static decimal SumBetween(Dictionary<DateTime, decimal> series, DateTime fromInclusive, DateTime toExclusive)
    {
        var total = 0m;
        foreach (var point in series)
        {
            if (point.Key >= fromInclusive && point.Key < toExclusive) total += point.Value;
        }
        return total;
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

                // An empty result is returned as empty. The previous implementation substituted a
                // fixed six-category dataset (Electronics 13768, Fashion 10723, ...) whenever the
                // tenant had no sales, so an empty tenant rendered invented revenue on the dashboard.
                return data
                    .Select(d => new SalesByCategoryDto(d.Category, d.Amount, total == 0 ? 0 : Math.Round((double)(d.Amount / total * 100), 1)))
                    .OrderByDescending(x => x.Amount)
                    .ToList();
            });

    public Task<List<PaymentMethodBreakdownDto>> GetPaymentMethodBreakdown() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}payment-methods",
            SnapshotLifetime,
            async ct =>
            {
                // Measured from recorded payments. The previous implementation ignored the Payment
                // table entirely and returned five hard-coded percentages (Cash 42.3%, Card 28.7%,
                // ...) against an invented 48520 total, so the card never reflected real activity.
                var data = await _context.Payments
                    .AsNoTracking()
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Amount) })
                    .ToListAsync(ct);

                var total = data.Sum(x => x.Amount);

                return data
                    .Select(d => new PaymentMethodBreakdownDto(
                        d.Method.ToString(),
                        Math.Round(d.Amount, 2),
                        total == 0 ? 0 : Math.Round((double)(d.Amount / total * 100), 1)))
                    .OrderByDescending(x => x.Amount)
                    .ToList();
            });

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

                var totalProducts = perProduct.Count;
                var inStock = perProduct.Count(p => p.TotalQuantity > 0);
                var lowStock = perProduct.Count(p =>
                    p.ReorderStockLevel > 0 && p.TotalQuantity > 0 && p.TotalQuantity <= p.ReorderStockLevel);
                var outOfStock = totalProducts - inStock;

                static double Pct(int value, int total) => total == 0 ? 0 : Math.Round((double)value / total * 100, 1);

                // No seeded fallback: an empty catalogue reports zero of everything, which is the
                // truth. It previously reported 2482 / 2124 / 198 / 160 regardless of the tenant.
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

                return data.Select(d => new DailySalesByStoreDto(d.Store, d.Sales)).ToList();
            });

    public Task<List<BestStoreDto>> GetBestPerformingStores() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}best-stores",
            SnapshotLifetime,
            async ct =>
            {
                // Growth is the real week-over-week change: the same branch grouping is run over the
                // previous seven days and the two totals are compared. The previous implementation
                // returned `new Random(42).NextDouble() * 10 + 5` per row, which re-randomised on
                // every cache rebuild and bore no relation to the data it was displayed beside.
                var now = DateTime.UtcNow;
                var startOfWeek = now.AddDays(-7);
                var prevWeekStart = now.AddDays(-14);

                var current = await BuildStoreTotalsAsync(now, startOfWeek, ct);
                var previous = await BuildStoreTotalsAsync(startOfWeek, prevWeekStart, ct);

                return current
                    .Select(entry =>
                    {
                        previous.TryGetValue(entry.Key, out var prior);
                        var growth = prior > 0m
                            ? Math.Round((double)((entry.Value - prior) / prior * 100), 1)
                            : 0d;
                        return new BestStoreDto(entry.Key, entry.Value, growth);
                    })
                    .ToList();
            });

    private async Task<Dictionary<string, decimal>> BuildStoreTotalsAsync(
        DateTime from, DateTime to, CancellationToken ct)
    {
        var rows = await _context.SalesOrders
            .AsNoTracking()
            .Where(o => o.OrderDate >= from && o.OrderDate < to && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.Branch != null ? o.Branch.Name : "Unassigned")
            .Select(g => new { Store = g.Key, Sales = g.Sum(o => o.TotalAmount) })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.Store, x => x.Sales);
    }

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

                // Only conditions that are actually detected are reported. The previous
                // implementation unconditionally appended three invented alerts ("New customer
                // registration", "Supplier payment due: ABC Supplier", "System backup completed")
                // to every tenant's feed regardless of whether any such event had occurred.
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

    // ── Purchase Overview ─────────────────────────────────────────────

    public Task<List<PurchaseOverviewPointDto>> GetPurchaseOverview(int days) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}purchase-overview:{days}",
            SnapshotLifetime,
            ct => BuildPurchaseOverviewAsync(days, ct));

    private async Task<List<PurchaseOverviewPointDto>> BuildPurchaseOverviewAsync(int days, CancellationToken ct)
    {
        days = Math.Clamp(days, 1, MaxOverviewDays);

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-days);
        var firstDay = now.Date.AddDays(-(days - 1));

        // Draft and Cancelled purchase orders are excluded: neither represents committed spend, and
        // including drafts would make procurement look busier than it is.
        var rows = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.PurchaseDate >= cutoff
                     && p.Status != PurchaseOrderStatus.Cancelled
                     && p.Status != PurchaseOrderStatus.Draft)
            .GroupBy(p => p.PurchaseDate.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(p => p.TotalAmount), Count = g.Count() })
            .ToListAsync(ct);

        var byDay = new Dictionary<DateTime, (decimal Amount, int Count)>();
        foreach (var row in rows) byDay[row.Date.Date] = (row.Amount, row.Count);

        // Months with no orders at all are still emitted, so a gap in activity reads as a zero
        // rather than silently shortening the strip.
        if (days > MonthlyBucketThresholdDays)
        {
            // The final month is cut off at today. A future-dated order in the current month would
            // otherwise be counted in the bucket while the daily view, which stops at today, drops it.
            var endOfToday = now.Date.AddDays(1);
            var monthly = new List<PurchaseOverviewPointDto>();
            for (var month = new DateTime(firstDay.Year, firstDay.Month, 1); month <= now; month = month.AddMonths(1))
            {
                var next = month.AddMonths(1);
                if (next > endOfToday) next = endOfToday;

                var amount = 0m;
                var count = 0;
                foreach (var day in byDay)
                {
                    if (day.Key < month || day.Key >= next) continue;
                    amount += day.Value.Amount;
                    count += day.Value.Count;
                }
                monthly.Add(new PurchaseOverviewPointDto(month.ToString("MMM yyyy"), amount, count));
            }
            return monthly;
        }

        // Every day in the window is emitted, including days with no orders, so the strip keeps a
        // stable width instead of collapsing when a day happens to be empty.
        var result = new List<PurchaseOverviewPointDto>(days);
        for (var offset = days - 1; offset >= 0; offset--)
        {
            var day = firstDay.AddDays(-offset);
            byDay.TryGetValue(day, out var hit);
            result.Add(new PurchaseOverviewPointDto(day.ToString("MMM dd"), hit.Amount, hit.Count));
        }

        return result;
    }

    // ── Inventory Overview ────────────────────────────────────────────

    public Task<InventoryOverviewDto> GetInventoryOverview() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}inventory-overview",
            SnapshotLifetime,
            ct => BuildInventoryOverviewAsync(ct));

    private async Task<InventoryOverviewDto> BuildInventoryOverviewAsync(CancellationToken ct)
    {
        // Two shapes of aggregate, because they answer different questions. The per-product roll-up
        // counts *products* by health, so a product stocked in three warehouses is counted once
        // rather than three times. The single-row group sums *units and value* across every
        // inventory row. A product with no stock rows at all still appears in the first query with a
        // total of zero, and therefore counts as out of stock.
        var perProduct = await _context.Products
            .AsNoTracking()
            .Select(p => new
            {
                p.ReorderStockLevel,
                OnHand = p.Inventories.Sum(i => i.Quantity)
            })
            .ToListAsync(ct);

        var totals = await _context.Inventories
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                OnHand = g.Sum(i => i.Quantity),
                Reserved = g.Sum(i => i.ReservedQuantity),
                AtCost = g.Sum(i => (decimal?)(i.Quantity * i.Product.CostPrice)),
                AtRetail = g.Sum(i => (decimal?)(i.Quantity * i.Product.SellingPrice))
            })
            .FirstOrDefaultAsync(ct);

        var totalProducts = perProduct.Count;
        var inStock = perProduct.Count(p => p.OnHand > 0);
        var lowStock = perProduct.Count(p =>
            p.ReorderStockLevel > 0 && p.OnHand > 0 && p.OnHand <= p.ReorderStockLevel);
        var outOfStock = totalProducts - inStock;

        var onHand = totals?.OnHand ?? 0;
        var reserved = totals?.Reserved ?? 0;

        return new InventoryOverviewDto(
            totalProducts, inStock, lowStock, outOfStock,
            onHand, reserved, onHand - reserved,
            Math.Round(totals?.AtCost ?? 0m, 2),
            Math.Round(totals?.AtRetail ?? 0m, 2));
    }

    // ── Store Overview ────────────────────────────────────────────────

    public Task<List<StoreOverviewDto>> GetStoreOverview() =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}store-overview",
            SnapshotLifetime,
            ct => BuildStoreOverviewAsync(ct));

    private async Task<List<StoreOverviewDto>> BuildStoreOverviewAsync(CancellationToken ct)
    {
        // Grouped over sales order *lines* rather than order headers so that revenue and cost are
        // measured on the same rows. Summing the header TotalAmount would not allow the cost side of
        // the margin to be derived at all, and joining through Branch keeps unattributed orders in
        // the result as "Unassigned" instead of silently dropping them.
        var rows = await _context.SalesOrderItems
            .AsNoTracking()
            .Where(i => i.SalesOrder.Status != OrderStatus.Cancelled)
            .GroupBy(i => i.SalesOrder.Branch != null ? i.SalesOrder.Branch.Name : "Unassigned")
            .Select(g => new
            {
                Store = g.Key,
                Revenue = g.Sum(i => i.LineTotalPrice),
                Cost = g.Sum(i => i.Product.CostPrice * i.Quantity),
                Orders = g.Select(i => i.SalesOrderId).Distinct().Count()
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(ct);

        var total = rows.Sum(x => x.Revenue);

        return rows.Select(r =>
        {
            var revenue = Math.Round(r.Revenue, 2);
            var cost = Math.Round(r.Cost, 2);
            return new StoreOverviewDto(
                r.Store,
                revenue,
                cost,
                Math.Round(revenue - cost, 2),
                r.Orders,
                total == 0 ? 0 : Math.Round((double)(r.Revenue / total * 100), 1));
        }).ToList();
    }

    // ── Stock Levels ──────────────────────────────────────────────────

    public Task<List<StockLevelDto>> GetStockLevels(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}stock-levels:{count}",
            SnapshotLifetime,
            ct => BuildStockLevelsAsync(count, ct));

    private async Task<List<StockLevelDto>> BuildStockLevelsAsync(int count, CancellationToken ct)
    {
        count = Math.Clamp(count, 1, 50);

        // Ordering is done in the database rather than after materialisation so the "most at risk
        // first" ranking holds for the whole table, not just the rows that survived the Take. The
        // CASE expression pushes rows at or below their reorder level to the front, then the
        // product name gives a stable secondary order.
        return await _context.Inventories
            .AsNoTracking()
            .OrderBy(i => i.Quantity <= i.Product.ReorderStockLevel ? 0 : 1)
            .ThenBy(i => i.Product.Name)
            .ThenBy(i => i.Warehouse.Name)
            .Take(count)
            .Select(i => new StockLevelDto(
                i.Id,
                i.ProductId,
                i.Product.Name,
                i.Product.SKU,
                i.WarehouseId,
                i.Warehouse.Name,
                i.Quantity,
                i.ReservedQuantity,
                i.Quantity - i.ReservedQuantity,
                i.Product.ReorderStockLevel,
                i.BinLocation))
            .ToListAsync(ct);
    }

    // ── Recent Activities ─────────────────────────────────────────────

    public Task<List<RecentActivityDto>> GetRecentActivities(int count) =>
        _cache.GetOrCreateAsync(
            $"{CachePrefix}recent-activities:{count}",
            SnapshotLifetime,
            ct => BuildRecentActivitiesAsync(count, ct));

    private async Task<List<RecentActivityDto>> BuildRecentActivitiesAsync(int count, CancellationToken ct)
    {
        count = Math.Clamp(count, 1, 50);

        // AuditLog is deliberately NOT derived from BaseEntity, so the DbContext's tenant query
        // filter does not apply to it and no implicit TenantId predicate reaches the query. The
        // tenant must therefore be applied explicitly here. Anything reading the audit log without
        // this predicate returns every tenant's change history, including the serialised old and new
        // values of every record they touched.
        var tenantId = _tenantProvider.GetTenantId();

        var rows = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .Select(a => new { a.TableName, a.Action, a.UserId, a.Timestamp, a.RecordId })
            .ToListAsync(ct);

        return rows
            .Select(r => new RecentActivityDto(
                DescribeActivity(r.Action, r.TableName),
                r.RecordId == Guid.Empty ? "" : $"#{r.RecordId.ToString()[..8].ToUpperInvariant()}",
                string.IsNullOrWhiteSpace(r.UserId) ? "system" : r.UserId!,
                GetTimeAgo(r.Timestamp),
                ActivityIcon(r.Action),
                ActivityTone(r.Action)))
            .ToList();
    }

    /// <summary>Renders an audit row as a human sentence, e.g. "Created Sales Order".</summary>
    private static string DescribeActivity(string? action, string? tableName)
    {
        var verb = Normalize(action);
        var noun = SplitPascalCase(tableName);

        if (string.IsNullOrEmpty(noun)) return verb;
        if (string.IsNullOrEmpty(verb)) return noun;

        return $"{verb} {noun}";
    }

    /// <summary>
    /// Inserts a space before each interior capital so "SalesOrder" reads as "Sales Order". An
    /// acronym run is left intact, so "SKU" does not become "S K U".
    /// </summary>
    private static string SplitPascalCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i]) && !char.IsUpper(value[i - 1]))
                builder.Append(' ');

            builder.Append(value[i]);
        }

        return builder.ToString();
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    // Material icon *names* as plain strings. The Infrastructure project does not reference
    // MudBlazor, and the DTO layer stays UI-framework free; the Razor cards resolve these against
    // MudBlazor's Icons.Material.Filled lookup. This matches how SystemAlertDto.Icon already works.
    private static string ActivityIcon(string? action) => action switch
    {
        "Created" => "AddCircle",
        "Updated" => "EditNote",
        "Deleted" => "DeleteOutline",
        _ => "History"
    };

    private static string ActivityTone(string? action) => action switch
    {
        "Created" => "#22c55e",
        "Updated" => "#3b82f6",
        "Deleted" => "#ef4444",
        _ => "#94a3b8"
    };
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
