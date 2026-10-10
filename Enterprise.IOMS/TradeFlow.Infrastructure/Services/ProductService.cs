using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TradeFlow.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        // The brand, category and warehouse reference lists that used to be cached here now live in
        // BrandService, CategoryService and WarehousesService behind ReferenceDataCache, so this
        // service keeps only its own catalogue prefix. The IDbContextFactory went with them too: its
        // only user was the warehouse paged read.
        private const string ProductCachePrefix = "products:";

        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ITenantCache _cache;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            ApplicationDbContext db,
            IMapper mapper,
            ITenantCache cache,
            ITenantProvider tenantProvider,
            ILogger<ProductService> logger)
        {
            _db = db;
            _mapper = mapper;
            _cache = cache;
            _tenantProvider = tenantProvider;
            _logger = logger;
        }

        public async Task<PagedResultNew<ProductDto>> GetProductsAsync(ProductPagedRequest request)
        {
            var page = Math.Max(0, request.CurrentPage);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var search = request.SearchTerm?.Trim();

            // Tenant scoping comes from the global query filter, which is driven by the
            // authenticated principal. A caller-supplied TenantId is honoured only when it matches
            // the caller's own tenant, so a request can never widen its visibility.
            var tenantId = _tenantProvider.GetTenantId();
            var requestedTenant = request.TenantId;
            if (requestedTenant.HasValue && requestedTenant.Value != Guid.Empty && requestedTenant.Value != tenantId)
            {
                _logger.LogWarning(
                    "Product list requested tenant {RequestedTenant} but the caller belongs to {ActualTenant}; scoping to the caller's tenant.",
                    requestedTenant.Value,
                    tenantId);
            }

            // No IgnoreQueryFilters(): the DbContext filter already applies TenantId and IsDeleted.
            var baseQuery = _db.Products.AsNoTracking();

            // Aggregate stock once per product instead of re-evaluating the correlated
            // Inventories.Sum(...) subquery inside every filter, count, and projection.
            var query = baseQuery
                .Select(p => new
                {
                    Product = p,
                    TotalStock = p.Inventories.Sum(i => i.Quantity)
                });

            // Header counts describe the tenant's catalogue, not the current filter, so they run
            // over the unfiltered set.
            //
            // NOTE: these queries are issued sequentially, not via Task.WhenAll. EF Core's
            // DbContext is explicitly not thread-safe and throws "A second operation was started
            // on this context instance" if two commands overlap on the same instance. Real
            // parallelism would require a separate DbContext per query (IDbContextFactory).
            var stats = await BuildStockStatsAsync(baseQuery);

            var filtered = request.Status switch
            {
                StockFilter.InStock => query.Where(x => x.TotalStock > x.Product.ReorderStockLevel && x.TotalStock > 0),
                StockFilter.LowStock => query.Where(x => x.TotalStock > 0 && x.TotalStock <= x.Product.ReorderStockLevel),
                StockFilter.OutOfStock => query.Where(x => x.TotalStock <= 0),
                _ => query
            };

            if (request.CategoryId is { } categoryId && categoryId != Guid.Empty)
                filtered = filtered.Where(x => x.Product.CategoryId == categoryId);

            if (request.BrandId is { } brandId && brandId != Guid.Empty)
                filtered = filtered.Where(x => x.Product.BrandId == brandId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                // Sargable search: plain Contains compiles to LIKE and can use an index, whereas
                // the previous ToLower().Contains forced a function on the column. The decimal
                // columns are dropped from the predicate because casting them to text for a
                // substring match guaranteed a full scan.
                var term = search;
                filtered = filtered.Where(x =>
                    x.Product.Name.Contains(term) ||
                    x.Product.SKU.Contains(term) ||
                    (x.Product.Barcode != null && x.Product.Barcode.Contains(term)) ||
                    (x.Product.Model != null && x.Product.Model.Contains(term)) ||
                    (x.Product.Description != null && x.Product.Description.Contains(term)));
            }

            // Count the filtered set, not the whole catalogue. Counting before the filters were
            // applied reported the unfiltered total, so a narrow search produced phantom pages.
            var totalCount = (int)Math.Min(
                await filtered.LongCountAsync(),
                int.MaxValue);

            var items = await filtered
                .OrderByDescending(x => x.Product.CreatedAt)
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(x => new ProductDto(
                    x.Product.Id,
                    x.Product.Name,
                    x.Product.SKU,
                    x.Product.Barcode,
                    x.Product.Description,
                    x.Product.CostPrice,
                    x.Product.SellingPrice,
                    x.Product.WholeSellingPrice,
                    x.Product.ReorderStockLevel,
                    x.Product.MinOrderQuantity,
                    x.Product.WarrantyInMonths,
                    x.Product.CategoryId,
                    x.Product.Category != null ? x.Product.Category.Path:null,
                    x.Product.BrandId,
                    x.Product.Brand != null ? x.Product.Brand.Name : null,
                    x.Product.Model,
                    x.Product.ImageUrl,
                    x.TotalStock,
                    x.Product.IsKit,
                    x.Product.OriginCountry ?? string.Empty,
                    x.Product.OriginManufacturer ?? string.Empty,
                    x.Product.Inventories
                        .OrderByDescending(i => i.Quantity)
                        .Select(i => new ProductWarehouseStockDto(
                            i.WarehouseId,
                            i.Warehouse != null ? i.Warehouse.Name : "—",
                            i.Quantity,
                            i.ReservedQuantity,
                            i.Quantity - i.ReservedQuantity))
                        .ToList(),
                    x.Product.Category != null && x.Product.Category.ParentCategory != null
                        ? x.Product.Category.ParentCategory.Name : null,
                    x.Product.Category != null ? x.Product.Category.Name : null,
                    x.Product.Inventories.Sum(i => i.Quantity - i.ReservedQuantity),
                    x.Product.SalesOrderItems
                        .OrderByDescending(soi => soi.SalesOrder.OrderDate)
                        .Select(soi => (decimal?)soi.UnitPrice)
                        .FirstOrDefault()))
                .ToListAsync();

            return new PagedResultNew<ProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                Stats = stats
            };
        }

        /// <summary>
        /// Computes the header stock counts in a single round trip. The previous implementation
        /// issued four separate queries, each re-evaluating <c>Inventories.Sum(...)</c> per row.
        /// </summary>
        private static async Task<Dictionary<string, int>> BuildStockStatsAsync(IQueryable<Product> products)
        {
            var stats = await products
                .GroupBy(p => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    InStock = g.Count(p => p.Inventories.Sum(i => i.Quantity) > p.ReorderStockLevel
                                        && p.Inventories.Sum(i => i.Quantity) > 0),
                    LowStock = g.Count(p => p.Inventories.Sum(i => i.Quantity) > 0
                                        && p.Inventories.Sum(i => i.Quantity) <= p.ReorderStockLevel),
                    OutOfStock = g.Count(p => p.Inventories.Sum(i => i.Quantity) <= 0)
                })
                .FirstOrDefaultAsync();

            return new Dictionary<string, int>
            {
                ["TotalCount"] = stats?.Total ?? 0,
                ["InStockCount"] = stats?.InStock ?? 0,
                ["LowStockCount"] = stats?.LowStock ?? 0,
                ["OutOfStockCount"] = stats?.OutOfStock ?? 0
            };
        }

        /// <summary>
        /// Lightweight product list for pickers. A dozen components used to run
        /// <c>DbContext.Products.ToListAsync()</c> — no filter, no ordering, tracked entities
        /// included — purely to fill a dropdown that reads four columns.
        /// </summary>
        public async Task<List<ProductOptionDto>> GetProductOptionsAsync() =>
            await _db.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new ProductOptionDto(p.Id, p.Name, p.SKU, p.SellingPrice, p.CostPrice))
                .ToListAsync();

        public async Task<ProductDto?> GetProductByWarehouseIdAsync(Guid id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductDto(
                    p.Id, p.Name, p.SKU, p.Barcode, p.Description, p.CostPrice, p.SellingPrice,
                    p.WholeSellingPrice, p.ReorderStockLevel, p.MinOrderQuantity, p.WarrantyInMonths, p.CategoryId,
                    p.Category != null ? p.Category.Name : null,
                    p.BrandId, p.Brand != null ? p.Brand.Name : null, p.Model, p.ImageUrl,
                    p.Inventories.Sum(i => i.Quantity), p.IsKit,
                    p.OriginCountry ?? string.Empty, p.OriginManufacturer ?? string.Empty))
                .FirstOrDefaultAsync();
            return product;
        }

        public async Task<ProductDto?> GetProductByIdAsync(Guid id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductDto(
                    p.Id, p.Name, p.SKU, p.Barcode, p.Description, p.CostPrice, p.SellingPrice,
                    p.WholeSellingPrice, p.ReorderStockLevel, p.MinOrderQuantity, p.WarrantyInMonths, p.CategoryId,
                    p.Category != null ? p.Category.Name : null,
                    p.BrandId, p.Brand != null ? p.Brand.Name : null, p.Model, p.ImageUrl,
                    p.Inventories.Sum(i => i.Quantity), p.IsKit,
                    p.OriginCountry ?? string.Empty, p.OriginManufacturer ?? string.Empty))
                .FirstOrDefaultAsync();
            return product;
        }

        public async Task<ProductDetailsDto?> GetProductDetailsByIdAsync(Guid id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Inventories)
                .FirstOrDefaultAsync(p => p.Id == id);
            return product == null ? null : _mapper.Map<ProductDetailsDto>(product);
        }

        public async Task<Guid> CreateProductAsync(ProductDetailsDto dto)
        {
         //   var barcodeValue = $"IOMS{dto.Id:D10}";

            var product = new Product
            {
                Name = dto.Name,
                SKU = dto.SKU,
                Barcode = dto.Barcode,
                CategoryId = dto.CategoryId,
                BrandId = dto.BrandId,
                Model = dto.Model,
                CostPrice = dto.CostPrice,
                SellingPrice = dto.SellingPrice,
                WholeSellingPrice = dto.WholeSellingPrice,
                ReorderStockLevel = dto.ReorderStockLevel,
                OriginManufacturer = dto.OriginManufacturer,
                OriginCountry = dto.OriginCountry,
                MinOrderQuantity = dto.MinOrderQuantity,
                BaseUoMId = dto.BaseUoMId,
                ImageUrl = dto.ImageUrl,
                Description = dto.Description
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            await InvalidateProductCachesAsync();
            _logger.LogInformation("Created product {ProductId} (SKU {Sku})", product.Id, product.SKU);
            return product.Id;
        }

        public Task<Guid> CreateProductDetailsAsync(ProductDetailsDto dto) => CreateProductAsync(dto);

        public async Task UpdateProductAsync(ProductDetailsDto dto)
        {
            var product = await _db.Products.FindAsync(dto.Id) ?? throw new KeyNotFoundException("Product not found");
            ApplyChanges(product, dto);
            await _db.SaveChangesAsync();
            await InvalidateProductCachesAsync();
            _logger.LogInformation("Updated product {ProductId} (SKU {Sku})", product.Id, product.SKU);
        }

        public Task UpdateProductDetailsAsync(ProductDetailsDto dto) => UpdateProductAsync(dto);

        public async Task DeleteProductAsync(Guid id)
        {
            var product = await _db.Products.FindAsync(id) ?? throw new KeyNotFoundException("Product not found");
            product.IsDeleted = true;
            await _db.SaveChangesAsync();
            await InvalidateProductCachesAsync();
            _logger.LogInformation("Soft-deleted product {ProductId}", id);
        }

        /// <summary>
        /// Assembles the product detail screen in one place.
        /// </summary>
        /// <remarks>
        /// The page used to issue eight queries against its own scoped context, starting with a
        /// tracked product that pulled in the category tree and the whole inventory graph. That
        /// tracked entity was then handed straight to the Save Changes button, which re-saved an
        /// unmodified graph. Everything here is <c>AsNoTracking</c> and projected, and the screen
        /// is read-only, so there is nothing left to persist.
        /// </remarks>
        public async Task<ProductDetailViewDto?> GetProductDetailViewAsync(Guid id)
        {
            var header = await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.SKU,
                    p.Barcode,
                    p.Description,
                    p.ImageUrl,
                    p.CostPrice,
                    p.SellingPrice,
                    p.WholeSellingPrice,
                    p.ReorderStockLevel,
                    p.MinOrderQuantity,
                    p.WarrantyInMonths,
                    p.Model,
                    p.OriginCountry,
                    p.OriginManufacturer,
                    CategoryName = p.Category.Name,
                    ParentCategoryName = p.Category.ParentCategory == null ? null : p.Category.ParentCategory.Name,
                    BrandName = p.Brand == null ? null : p.Brand.Name
                })
                .FirstOrDefaultAsync();

            if (header == null)
                return null;

            var warehouseStock = await _db.Inventories
                .AsNoTracking()
                .Where(i => i.ProductId == id)
                .OrderBy(i => i.Warehouse.Name)
                .Select(i => new ProductWarehouseStockDto(
                    i.WarehouseId,
                    i.Warehouse.Name,
                    i.Quantity,
                    i.ReservedQuantity,
                    i.Quantity - i.ReservedQuantity))
                .ToListAsync();

            var movements = await _db.StockMovements
                .AsNoTracking()
                .Where(m => m.ProductId == id)
                .OrderByDescending(m => m.MovementDate)
                .Take(50)
                .Select(m => new StockMovementReportRowDto(
                    m.MovementDate,
                    "",
                    m.Warehouse.Name,
                    m.Type.ToString(),
                    m.Quantity,
                    m.Reference,
                    m.Notes))
                .ToListAsync();

            var cutoff = DateTime.UtcNow.AddDays(-30);
            var last30DaysSales = await _db.SalesOrderItems
                .Where(x => x.ProductId == id && x.SalesOrder.OrderDate >= cutoff)
                .SumAsync(x => (int?)x.Quantity) ?? 0;

            var salesOrderLines = await _db.SalesOrderItems
                .AsNoTracking()
                .Where(x => x.ProductId == id)
                .OrderByDescending(x => x.SalesOrder.OrderDate)
                .Take(50)
                .Select(x => new ProductOrderLineDto(
                    x.SalesOrderId,
                    x.SalesOrder.OrderNumber,
                    x.SalesOrder.Customer.CustomerName,
                    x.SalesOrder.OrderDate,
                    x.SalesOrder.Status.ToString(),
                    x.Quantity,
                    x.UnitPrice))
                .ToListAsync();

            var purchaseOrderLines = await _db.PurchaseOrderItems
                .AsNoTracking()
                .Where(x => x.ProductId == id)
                .OrderByDescending(x => x.PurchaseOrder.PurchaseDate)
                .Take(50)
                .Select(x => new ProductOrderLineDto(
                    x.PurchaseOrderId,
                    x.PurchaseOrder.OrderNumber,
                    x.PurchaseOrder.Supplier.SupplierName,
                    x.PurchaseOrder.PurchaseDate,
                    x.PurchaseOrder.Status.ToString(),
                    x.Quantity,
                    x.UnitPrice))
                .ToListAsync();

            // The supplier on the most recent purchase line is the product's preferred source.
            var preferredSupplierName = await _db.PurchaseOrderItems
                .Where(x => x.ProductId == id)
                .OrderByDescending(x => x.PurchaseOrder.PurchaseDate)
                .Select(x => x.PurchaseOrder.Supplier.SupplierName)
                .FirstOrDefaultAsync();

            return new ProductDetailViewDto(
                header.Id,
                header.Name,
                header.SKU,
                header.Barcode,
                header.Description,
                header.ImageUrl,
                header.CostPrice,
                header.SellingPrice,
                header.WholeSellingPrice,
                header.ReorderStockLevel,
                header.MinOrderQuantity,
                header.WarrantyInMonths,
                header.Model,
                header.OriginCountry,
                header.OriginManufacturer,
                header.CategoryName,
                header.ParentCategoryName,
                header.BrandName,
                preferredSupplierName,
                last30DaysSales,
                warehouseStock.Sum(i => i.AvailableQuantity),
                warehouseStock,
                movements,
                salesOrderLines,
                purchaseOrderLines);
        }

        /// <summary>Stock and catalogue changes alter both the reference lists and the stock counts.</summary>
        private Task InvalidateProductCachesAsync() => _cache.RemoveByPrefixAsync(ProductCachePrefix);

        private static void ApplyChanges(Product product, ProductDetailsDto dto)
        {
            product.Name = dto.Name;
            product.SKU = dto.SKU;
            product.Barcode = dto.Barcode;
            product.CategoryId = dto.CategoryId;
            product.BrandId = dto.BrandId;
            product.Model = dto.Model;
            product.CostPrice = dto.CostPrice;
            product.SellingPrice = dto.SellingPrice;
            product.WholeSellingPrice = dto.WholeSellingPrice;
            product.ReorderStockLevel = dto.ReorderStockLevel;
            product.OriginManufacturer = dto.OriginManufacturer;
            product.OriginCountry = dto.OriginCountry;
            product.MinOrderQuantity = dto.MinOrderQuantity;
            product.BaseUoMId = dto.BaseUoMId;
            product.ImageUrl = dto.ImageUrl;
            product.Description = dto.Description;
        }
    }
}
