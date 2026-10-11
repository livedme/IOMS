using AutoMapper;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Exceptions;
using TradeFlow.Infrastructure.Data;
using TradeFlow.Shared.Helpers;
using static MudBlazor.CategoryTypes;

namespace TradeFlow.Infrastructure.Services;

// ─── Search Service ─────────────────────────────────────────────────────────
public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SearchService> _logger;
    private readonly IMapper _mapper;
    public SearchService(ApplicationDbContext context, ILogger<SearchService> logger, IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<GlobalSearchResultDto> Search(string query, IReadOnlyCollection<SearchEntity> entities, int maxResults = 20)
    {
        var response = new GlobalSearchResultDto();

        var started = Stopwatch.GetTimestamp();

        // Each selected kind shares the budget so a single-type search gets the full per-type depth,
        // and the five-way search behaves as before.
        //var selected = entities.Where(e => e != default).ToList();
        //if (selected.Count == 0) return response;
        var perType = maxResults;// Math.Max(maxResults / Math.Min(entities.Count, 5), 3);

        // Plain Contains compiles to LIKE and stays sargable; the previous ToLower().Contains
        // wrapped the column in a function and could not use an index.
        var term = query.Trim();

        // The searches are independent but are issued sequentially: EF Core's DbContext
        // rejects two concurrent operations on the same instance.
        if (entities.Contains(SearchEntity.Products) || entities.Count==0)
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.Name.Contains(term) || p.SKU.Contains(term)
                    || (p.Barcode != null && p.Barcode.Contains(term))
                    || (p.Model != null && p.Model.Contains(term))
                    || (p.Description != null && p.Description.Contains(term))
                    //|| (p.Category != null && p.Category.Name.Contains(term))
                    || (p.Brand != null && p.Brand.Name.Contains(term)))
                .OrderBy(p => p.Name)
                .Take(perType)
                .Select(x => new ProductDto(
                        x.Id,
                        x.Name,
                        x.SKU,
                        x.Barcode,
                        x.Description,
                        x.CostPrice,
                        x.SellingPrice,
                        x.WholeSellingPrice,
                        x.ReorderStockLevel,
                        x.MinOrderQuantity,
                        x.WarrantyInMonths,
                        x.CategoryId,
                        x.Category != null ? x.Category.Path : null,
                        x.BrandId,
                        x.Brand != null ? x.Brand.Name : null,
                        x.Model,
                        x.ImageUrl,
                        0,
                        x.IsKit,
                        x.OriginCountry ?? string.Empty,
                        x.OriginManufacturer ?? string.Empty,
                        x.Inventories
                            .OrderByDescending(i => i.Quantity)
                            .Select(i => new ProductWarehouseStockDto(
                                i.WarehouseId,
                                i.Warehouse != null ? i.Warehouse.Name : "—",
                                i.Quantity,
                                i.ReservedQuantity,
                                i.Quantity - i.ReservedQuantity))
                            .ToList(),
                        x.Category != null && x.Category.ParentCategory != null
                            ? x.Category.ParentCategory.Name : null,
                        x.Category != null ? x.Category.Name : null,
                        x.Inventories.Sum(i => i.Quantity - i.ReservedQuantity),
                        x.SalesOrderItems
                            .OrderByDescending(soi => soi.SalesOrder.OrderDate)
                            .Select(soi => (decimal?)soi.UnitPrice)
                            .FirstOrDefault())).ToListAsync();

            response.ProductItems.AddRange(products);
        }
        if (entities.Contains(SearchEntity.Category) || entities.Count == 0)
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p=>p.Category != null && p.Category.Name.Contains(term))
                .OrderBy(p => p.Name)
                .Take(perType)
                .Select(x => new ProductDto(
                        x.Id,
                        x.Name,
                        x.SKU,
                        x.Barcode,
                        x.Description,
                        x.CostPrice,
                        x.SellingPrice,
                        x.WholeSellingPrice,
                        x.ReorderStockLevel,
                        x.MinOrderQuantity,
                        x.WarrantyInMonths,
                        x.CategoryId,
                        x.Category != null ? x.Category.Path : null,
                        x.BrandId,
                        x.Brand != null ? x.Brand.Name : null,
                        x.Model,
                        x.ImageUrl,
                        0,
                        x.IsKit,
                        x.OriginCountry ?? string.Empty,
                        x.OriginManufacturer ?? string.Empty,
                        x.Inventories
                            .OrderByDescending(i => i.Quantity)
                            .Select(i => new ProductWarehouseStockDto(
                                i.WarehouseId,
                                i.Warehouse != null ? i.Warehouse.Name : "—",
                                i.Quantity,
                                i.ReservedQuantity,
                                i.Quantity - i.ReservedQuantity))
                            .ToList(),
                        x.Category != null && x.Category.ParentCategory != null
                            ? x.Category.ParentCategory.Name : null,
                        x.Category != null ? x.Category.Name : null,
                        x.Inventories.Sum(i => i.Quantity - i.ReservedQuantity),
                        x.SalesOrderItems
                            .OrderByDescending(soi => soi.SalesOrder.OrderDate)
                            .Select(soi => (decimal?)soi.UnitPrice)
                            .FirstOrDefault())).ToListAsync();

            response.ProductItems.AddRange(products);
        }

        if (entities.Contains(SearchEntity.Customers) || entities.Count == 0)
        {
            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => c.CustomerName.Contains(term) || (c.CustomerEmail != null && c.CustomerEmail.Contains(term)))
                .OrderBy(c => c.CustomerName)
                .Take(perType)
                .ToListAsync();

            response.CustomersItems.AddRange(_mapper.Map<List<CustomerDto>>(customers));
        }

        if (entities.Contains(SearchEntity.Suppliers))
        {
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.SupplierName.Contains(term) || (s.SupplierEmail != null && s.SupplierEmail.Contains(term)))
                .OrderBy(s => s.SupplierName)
                .Take(perType)
                .ToListAsync();

            response.SuppliersItems.AddRange(_mapper.Map<List<SupplierDto>>(suppliers));
        }

        if (entities.Contains(SearchEntity.SalesOrders) || entities.Count == 0)
        {
            var salesOrders = await _context.SalesOrders
                .AsNoTracking()
                .Where(o => o.OrderNumber.Contains(term) || (o.Customer != null && o.Customer.CustomerName.Contains(term)))
                .OrderByDescending(o => o.OrderDate)
                .Take(perType)
                .ToListAsync();

            response.SalesOrdersItems.AddRange(_mapper.Map<List<SalesOrderDto>>(salesOrders));
        }

        if (entities.Contains(SearchEntity.PurchaseOrders) || entities.Count == 0)
        {
            var purchaseOrders = await _context.PurchaseOrders
                .AsNoTracking()
                .Where(p => p.OrderNumber.Contains(term) || (p.Supplier != null && p.Supplier.SupplierName.Contains(term)))
                .OrderByDescending(p => p.PurchaseDate)
                .Take(perType)
                .ToListAsync();

            response.PurchaseOrdersItems.AddRange(_mapper.Map<List<PurchaseOrderDto>>(purchaseOrders));
        }

        _logger.LogDebug(
            "Global search for '{Query}' across {EntityCount} entity types returned {ResultCount} results in {ElapsedMs:F0} ms",
            term,
            entities.Count,
            response.ProductItems.Count + response.CustomersItems.Count
                + response.SuppliersItems.Count + response.SalesOrdersItems.Count + response.PurchaseOrdersItems.Count,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);

        return response;
    }
}
