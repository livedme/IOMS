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
