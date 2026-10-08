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
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ITenantCache _cache;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<CategoryService> _logger;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public CategoryService(
            ApplicationDbContext db,
            IMapper mapper,
            ITenantCache cache,
            ITenantProvider tenantProvider,
            ILogger<CategoryService> logger,
            IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _db = db;
            _mapper = mapper;
            _cache = cache;
            _tenantProvider = tenantProvider;
            _logger = logger;
            _dbFactory = dbFactory;
        }

       
        
        public Task<List<CategoryDto>> GetAllCategoriesAsync() =>
            _cache.GetOrCreateAsync(
                $"{ReferenceDataCache.Prefix}categories",
                ReferenceDataCache.Lifetime,
                async ct =>
                {
                    var categories = await _db.Categories
                        .AsNoTracking()
                        .Include(c => c.ParentCategory)
                        .OrderBy(c => c.Name)
                        .ToListAsync(ct);
                    return _mapper.Map<List<CategoryDto>>(categories);
                });

        public async Task<PagedResult<CategoryDto>> GetCategoriesAsync(string? search, int page, int pageSize)
        {
            var query = _db.Categories.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<CategoryDto>(_mapper.Map<List<CategoryDto>>(items), total, page, pageSize);
        }

        public async Task<PagedResultNew<CategoryDto>> GetCategoriesPagedAsync(CategoryPagedRequest request)
        {
            await using var read = await _dbFactory.CreateDbContextAsync();
            var response = new PagedResultNew<CategoryDto>();

            var page = Math.Max(0, request.CurrentPage);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var search = request.SearchTerm?.Trim();

            //var query = read.Categories.Include(c => c.ParentCategory).AsNoTracking();
            //if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
            //    query = query.Where(c => c.TenantId == request.TenantId.Value);

            IQueryable<Category> query;
            if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
                query = read.Categories.Include(c => c.ParentCategory).IgnoreQueryFilters().Where(c => c.TenantId == request.TenantId.Value).AsNoTracking();
            else
                query = read.Categories.Include(c => c.ParentCategory).AsNoTracking();


            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    EF.Functions.Like(c.Name, $"%{search}%") ||
                    EF.Functions.Like(c.Description, $"%{search}%") ||
                    EF.Functions.Like(c.Path, $"%{search}%"));
            }

            var statRows = await query
                .GroupBy(c => new { c.ParentCategoryId, HasProducts = c.Products.Any() })
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            response.Stats["TotalCount"] = statRows.Sum(r => r.Count);
            response.Stats["TopLevelCount"] = statRows.Count(r => r.Key.ParentCategoryId == null);
            response.Stats["SubLevelCount"] = statRows.Count(r => r.Key.ParentCategoryId != null);
            response.Stats["WithProductsCount"] = statRows.Where(r => r.Key.HasProducts).Sum(r => r.Count);
            response.Stats["WithoutProductsCount"] = statRows.Where(r => !r.Key.HasProducts).Sum(r => r.Count);

            // Depth is the number of " > " separators in Path, which SQL cannot count cheaply and
            // MaxAsync throws on an empty set, so the (small) reference table is projected first.
            var paths = await query
                .Where(c => c.Path != null && c.Path != string.Empty)
                .Select(c => c.Path!)
                .ToListAsync();
            response.Stats["MaxDepth"] = paths.Count == 0
                ? 1
                : paths.Max(p => p.Count(ch => ch == '>') + 1);

            if (request.Level == 1)
                query = query.Where(c => c.ParentCategoryId == null);
            else if (request.Level == 2)
                query = query.Where(c => c.ParentCategoryId != null);

            if (request.ProductFilter == "With Products")
                query = query.Where(c => c.Products.Any());
            else if (request.ProductFilter == "Without Products")
                query = query.Where(c => !c.Products.Any());

            var sortAsc = request.SortAscending;
            query = (request.SortColumn ?? "Name") switch
            {
                "Description" => sortAsc ? query.OrderBy(c => c.Description) : query.OrderByDescending(c => c.Description),
                "Path" => sortAsc ? query.OrderBy(c => c.Path) : query.OrderByDescending(c => c.Path),
                "ProductCount" => sortAsc
                    ? query.OrderBy(c => c.Products.Count).ThenBy(c => c.Name)
                    : query.OrderByDescending(c => c.Products.Count).ThenBy(c => c.Name),
                _ => sortAsc ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Select(c => new { Category = c, ProductCount = c.Products.Count })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = items
                .Select(x => new CategoryDto(
                    x.Category.Id,
                    x.Category.Name,
                    x.Category.Description,
                    x.Category.ParentCategoryId,
                    x.Category.ParentCategory == null ? null : x.Category.ParentCategory.Name,
                    x.ProductCount,
                    x.Category.Path))
                .ToList();
            response.TotalCount = totalCount;
            response.CurrentPage = page;
            response.PageSize = pageSize;

            return response;
        }
        public async Task<Guid> CreateCategoryAsync(CreateCategoryDto dto)
        {
            var category = new Category { Name = dto.Name, Description = dto.Description, ParentCategoryId = dto.ParentCategoryId };
            category.Path = await BuildPathAsync(dto.ParentCategoryId, dto.Name);
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceDataCache.Prefix);
            return category.Id;
        }

        /// <summary>
        /// Materialised category path, e.g. "Hardware &gt; Fasteners &gt; Bolts".
        /// </summary>
        /// <remarks>
        /// Two pages used to build this inline before saving — Categories.razor and
        /// ProductAddEditDialog.razor — each hand-rolling the same grandparent walk and each able to
        /// save a category with an empty path. It is a category invariant rather than a view concern,
        /// so it lives with the write.
        /// </remarks>
        private async Task<string?> BuildPathAsync(Guid? parentCategoryId, string name)
        {
            if (!parentCategoryId.HasValue)
                return name;

            var parent = await _db.Categories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == parentCategoryId.Value);

            if (parent == null)
                return name;

            return parent.ParentCategory == null
                ? $"{parent.Name} > {name}"
                : $"{parent.ParentCategory.Name} > {parent.Name} > {name}";
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            var category = await _db.Categories.FindAsync(id) ?? throw new KeyNotFoundException("Category not found");
            category.IsDeleted = true;
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceDataCache.Prefix);
        }

        public async Task<CategoryDto> GetCategoryByIdAsync(Guid id)
        {
            var category = await _db.Categories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new KeyNotFoundException("Category not found");

            return new CategoryDto(
                category.Id,
                category.Name,
                category.Description,
                category.ParentCategoryId,
                category.ParentCategory?.Name,
                category.Products.Count,
                category.Path);
        }

        public async Task<Guid> UpdateCategoryAsync(Guid id, CreateCategoryDto dto)
        {
            var category = await _db.Categories.FindAsync(id) ?? throw new KeyNotFoundException("Category not found");

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.ParentCategoryId = dto.ParentCategoryId;
            category.Path = await BuildPathAsync(dto.ParentCategoryId, dto.Name);
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceDataCache.Prefix);

            return category.Id;
        }
    }
}
