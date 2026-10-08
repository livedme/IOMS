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
