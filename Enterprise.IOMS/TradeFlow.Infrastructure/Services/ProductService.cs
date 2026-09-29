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
        /// <summary>Reference lists change rarely and are read on every page render, so they are cached.</summary>
        private static readonly TimeSpan ReferenceDataLifetime = TimeSpan.FromMinutes(15);

        private const string ReferenceCachePrefix = "products:reference:";
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
                .OrderBy(x => x.Product.Name)
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
                    x.Product.CategoryId,
                    x.Product.Category != null ? x.Product.Category.Name : null,
                    x.Product.BrandId,
                    x.Product.Brand != null ? x.Product.Brand.Name : null,
                    x.Product.Model,
                    x.Product.ImageUrl,
                    x.TotalStock,
                    x.Product.IsKit,
                    x.Product.OriginCountry ?? string.Empty,
                    x.Product.OriginManufacturer ?? string.Empty))
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

        public async Task<ProductDto?> GetProductByIdAsync(Guid id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductDto(
                    p.Id, p.Name, p.SKU, p.Barcode, p.Description, p.CostPrice, p.SellingPrice,
                    p.WholeSellingPrice, p.ReorderStockLevel, p.MinOrderQuantity, p.CategoryId,
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

        public async Task<PagedResult<BrandDto>> GetBrandsAsync(string? search, int page, int pageSize)
        {
            var query = _db.Brands.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Name.Contains(search)
                    || (b.BrandCode != null && b.BrandCode.Contains(search))
                    || (b.Description != null && b.Description.Contains(search))
                    || (b.OriginCompany != null && b.OriginCompany.Contains(search))
                    || (b.OriginCountry != null && b.OriginCountry.Contains(search)));

            var total = await query.CountAsync();
            var items = await query.OrderBy(b => b.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<BrandDto>(_mapper.Map<List<BrandDto>>(items), total, page, pageSize);
        }

        public Task<List<BrandDto>> GetAllBrandsAsync() =>
            _cache.GetOrCreateAsync(
                $"{ReferenceCachePrefix}brands",
                ReferenceDataLifetime,
                async ct =>
                {
                    var brands = await _db.Brands.AsNoTracking().OrderBy(b => b.Name).ToListAsync(ct);
                    return _mapper.Map<List<BrandDto>>(brands);
                });

        public async Task<BrandDto> GetBrandByIdAsync(Guid id)
        {
            var brand = await _db.Brands.FindAsync(id);
            if (brand == null) throw new KeyNotFoundException("Brand not found");
            return _mapper.Map<BrandDto>(brand);
        }
        public async Task<Guid> CreateBrandAsync(CreateBrandDto dto)
        {
            var brand = new Brand
            {
                Name = dto.Name,
                BrandCode = dto.BrandCode,
                Description = dto.Description,
                LogoUrl = dto.LogoUrl,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status,
                OriginCompany = dto.OriginCompany,
                OriginCountry = dto.OriginCountry,
                FoundedYear = dto.FoundedYear
            };
            _db.Brands.Add(brand);
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceCachePrefix);
            return brand.Id;
        }
        public async Task<bool> UpdateBrandAsync(Guid id, CreateBrandDto dto)
        {
            var brand = await _db.Brands.FindAsync(id) ?? throw new KeyNotFoundException("Brand not found");
            brand.Name = dto.Name;
            brand.BrandCode = dto.BrandCode;
            brand.Description = dto.Description;
            brand.LogoUrl = dto.LogoUrl;
            brand.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status;
            brand.OriginCompany = dto.OriginCompany;
            brand.OriginCountry = dto.OriginCountry;
            brand.FoundedYear = dto.FoundedYear;
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceCachePrefix);
            return true;
        }
        public async Task<bool> DeleteBrandAsync(Guid id)
        {
            var brand = await _db.Brands.FindAsync(id) ?? throw new KeyNotFoundException("Brand not found");
            brand.IsDeleted = true;
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceCachePrefix);
            return true;
        }

        public async Task<PagedResult<CategoryDto>> GetCategoriesAsync(string? search, int page, int pageSize)
        {
            var query = _db.Categories.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<CategoryDto>(_mapper.Map<List<CategoryDto>>(items), total, page, pageSize);
        }


        public Task<List<CategoryDto>> GetAllCategoriesAsync() =>
            _cache.GetOrCreateAsync(
                $"{ReferenceCachePrefix}categories",
                ReferenceDataLifetime,
                async ct =>
                {
                    var categories = await _db.Categories
                        .AsNoTracking()
                        .Include(c => c.ParentCategory)
                        .OrderBy(c => c.Name)
                        .ToListAsync(ct);
                    return _mapper.Map<List<CategoryDto>>(categories);
                });

        public async Task<Guid> CreateCategoryAsync(CreateCategoryDto dto)
        {
            var category = new Category { Name = dto.Name, Description = dto.Description, ParentCategoryId = dto.ParentCategoryId };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceCachePrefix);
            return category.Id;
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            var category = await _db.Categories.FindAsync(id) ?? throw new KeyNotFoundException("Category not found");
            category.IsDeleted = true;
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceCachePrefix);
        }

        public async Task<PagedResult<WarehouseDto>> GetWarehousesAsync(string? search, int page, int pageSize)
        {
            var query = _db.Warehouses.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(w => w.Name.Contains(search) || w.Code.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderBy(w => w.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<WarehouseDto>(_mapper.Map<List<WarehouseDto>>(items), total, page, pageSize);
        }

        public Task<List<WarehouseDto>> GetAllWarehousesAsync() =>
            _cache.GetOrCreateAsync(
                $"{ReferenceCachePrefix}warehouses",
                ReferenceDataLifetime,
                async ct =>
                {
                    var warehouses = await _db.Warehouses.AsNoTracking()
                        .Where(w => w.IsActive)
                        .OrderBy(w => w.Name)
                        .ToListAsync(ct);
                    return _mapper.Map<List<WarehouseDto>>(warehouses);
                });

        public async Task<Guid> CreateWarehouseAsync(CreateWarehouseDto dto)
        {
            var warehouse = new Warehouse { Name = dto.Name, Code = dto.Code, Location = dto.Location, Address = dto.Address };
            _db.Warehouses.Add(warehouse);
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceCachePrefix);
            return warehouse.Id;
        }

        public async Task DeleteWarehouseAsync(Guid id)
        {
            var warehouse = await _db.Warehouses.FindAsync(id) ?? throw new KeyNotFoundException("Warehouse not found");
            warehouse.IsDeleted = true;
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceCachePrefix);
        }
    }
}
