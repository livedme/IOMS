using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Domain.Exceptions;
using IOMS.Infrastructure.Data;
using IOMS.Shared.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services;

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

        return new PagedResult<ShipmentDto>(items, total, page, pageSize);
    }
}

// ─── Dashboard Service ──────────────────────────────────────────────────────
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantCache _cache;

    public DashboardService(ApplicationDbContext context, ITenantCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<DashboardKpiDto> GetDashboardKPIs()
    {
        var cached = await _cache.GetAsync<DashboardKpiDto>("dashboard:kpis");
        if (cached is not null)
            return cached;

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalProducts = await _context.Products.CountAsync();
        var activeCustomers = await _context.Customers.CountAsync(c => c.IsActive);
        var activeSuppliers = await _context.Suppliers.CountAsync(s => s.IsActive);

        var salesThisMonth = await _context.SalesOrders
            .Where(o => o.OrderDate >= startOfMonth && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount);

        var purchasesThisMonth = await _context.PurchaseOrders
            .Where(p => p.PurchaseDate >= startOfMonth && p.Status != PurchaseOrderStatus.Cancelled)
            .SumAsync(p => p.TotalAmount);

        var pendingSO = await _context.SalesOrders.CountAsync(o => o.Status == OrderStatus.Pending);
        var pendingPO = await _context.PurchaseOrders.CountAsync(p => p.Status == PurchaseOrderStatus.Draft || p.Status == PurchaseOrderStatus.Submitted);

        var lowStock = await _context.Inventories
            .Include(i => i.Product)
            .CountAsync(i => i.Quantity <= i.Product.ReorderStockLevel && i.Product.ReorderStockLevel > 0);

        var arBalance = await _context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Sales && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => i.TotalAmount - i.PaidAmount);

        var apBalance = await _context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => i.TotalAmount - i.PaidAmount);

        var overdueInvoices = await _context.Invoices
            .CountAsync(i => i.DueDate < now && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled);

        var inventoryValue = await _context.Inventories
            .Include(i => i.Product)
            .SumAsync(i => i.Quantity * i.Product.CostPrice);

        var result = new DashboardKpiDto(
            totalProducts, activeCustomers, activeSuppliers,
            salesThisMonth, purchasesThisMonth,
            pendingSO, pendingPO, lowStock,
            arBalance, apBalance, overdueInvoices, inventoryValue);

        await _cache.SetAsync("dashboard:kpis", result, TimeSpan.FromSeconds(30));
        return result;
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

    public async Task<List<TopProductDto>> GetTopProducts(int count)
    {
        var data = await _context.SalesOrderItems
            .Include(i => i.Product)
            .GroupBy(i => new { i.ProductId, i.Product.Name })
            .Select(g => new { g.Key.Name, Revenue = g.Sum(i => i.Quantity * i.UnitPrice), Units = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Revenue)
            .Take(count)
            .ToListAsync();

        return data.Select(d => new TopProductDto(d.Name, d.Revenue, d.Units)).ToList();
    }

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
}

// ─── Search Service ─────────────────────────────────────────────────────────
public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;

    public SearchService(ApplicationDbContext context) => _context = context;

    public async Task<List<GlobalSearchResultDto>> Search(string query, int maxResults = 20)
    {
        var results = new List<GlobalSearchResultDto>();
        var q = query.ToLower();
        var perType = Math.Max(maxResults / 5, 3);

        var products = await _context.Products
            .Where(p => p.Name.ToLower().Contains(q) || p.SKU.ToLower().Contains(q))
            .Take(perType)
            .Select(p => new GlobalSearchResultDto("Product", p.Id, p.Name, p.SKU, $"/products"))
            .ToListAsync();
        results.AddRange(products);

        var customers = await _context.Customers
            .Where(c => c.CustomerName.ToLower().Contains(q) || (c.CustomerEmail != null && c.CustomerEmail.ToLower().Contains(q)))
            .Take(perType)
            .Select(c => new GlobalSearchResultDto("Customer", c.Id, c.CustomerName, c.CustomerEmail ?? "", $"/customers"))
            .ToListAsync();
        results.AddRange(customers);

        var suppliers = await _context.Suppliers
            .Where(s => s.SupplierName.ToLower().Contains(q) || (s.SupplierEmail != null && s.SupplierEmail.ToLower().Contains(q)))
            .Take(perType)
            .Select(s => new GlobalSearchResultDto("Supplier", s.Id, s.SupplierName, s.SupplierEmail ?? "", $"/suppliers"))
            .ToListAsync();
        results.AddRange(suppliers);

        var salesOrders = await _context.SalesOrders
            .Include(o => o.Customer)
            .Where(o => o.OrderNumber.ToLower().Contains(q) || o.Customer.CustomerName.ToLower().Contains(q))
            .Take(perType)
            .Select(o => new GlobalSearchResultDto("Sales Order", o.Id, o.OrderNumber, o.Customer.CustomerName, $"/orders/sales"))
            .ToListAsync();
        results.AddRange(salesOrders);

        var purchaseOrders = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Where(p => p.OrderNumber.ToLower().Contains(q) || p.Supplier.SupplierName.ToLower().Contains(q))
            .Take(perType)
            .Select(p => new GlobalSearchResultDto("Purchase Order", p.Id, p.OrderNumber, p.Supplier.SupplierName, $"/orders/purchase"))
            .ToListAsync();
        results.AddRange(purchaseOrders);

        return results.Take(maxResults).ToList();
    }
}
