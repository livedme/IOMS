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
    public class BrandService : IBrandService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ITenantCache _cache;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<BrandService> _logger;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public BrandService(
            ApplicationDbContext db,
            IMapper mapper,
            ITenantCache cache,
            ITenantProvider tenantProvider,
            ILogger<BrandService> logger,
            IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _db = db;
            _mapper = mapper;
            _cache = cache;
            _tenantProvider = tenantProvider;
            _logger = logger;
            _dbFactory = dbFactory;
        }

        public Task<List<BrandDto>> GetAllBrandsAsync() =>
            _cache.GetOrCreateAsync(
                $"{ReferenceDataCache.Prefix}brands",
                ReferenceDataCache.Lifetime,
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

        public async Task<PagedResultNew<BrandDto>> GetBrandsPagedAsync(BrandPagedRequest request)
        {
            await using var read = await _dbFactory.CreateDbContextAsync();
            var response = new PagedResultNew<BrandDto>();

            var page = Math.Max(0, request.CurrentPage);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var search = request.SearchTerm?.Trim();

            var query = read.Brands.AsNoTracking();
            if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
                query = query.Where(b => b.TenantId == request.TenantId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b =>
                    EF.Functions.Like(b.Name, $"%{search}%") ||
                    EF.Functions.Like(b.BrandCode, $"%{search}%") ||
                    EF.Functions.Like(b.Description, $"%{search}%") ||
                    EF.Functions.Like(b.OriginCompany, $"%{search}%") ||
                    EF.Functions.Like(b.OriginCountry, $"%{search}%"));
            }

            var statRows = await query
                .GroupBy(b => b.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            response.Stats["TotalCount"] = statRows.Sum(r => r.Count);
            response.Stats["ActiveCount"] = statRows.Where(r => r.Key == "Active").Sum(r => r.Count);
            response.Stats["InactiveCount"] = statRows.Where(r => r.Key != "Active").Sum(r => r.Count);
            response.Stats["WithProductsCount"] = await query.CountAsync(b => b.Products.Any());
            response.Stats["WithoutProductsCount"] = await query.CountAsync(b => !b.Products.Any());
            response.Stats["WithLogoCount"] = await query
                .CountAsync(b => b.LogoUrl != null && b.LogoUrl != string.Empty);
            response.Stats["CountryCount"] = await query
                .Where(b => b.OriginCountry != null && b.OriginCountry != string.Empty)
                .Select(b => b.OriginCountry)
                .Distinct()
                .CountAsync();

            if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "All")
            {
                var status = request.Status;
                query = query.Where(b => b.Status == status);
            }

            if (request.HasLogo.HasValue)
            {
                query = request.HasLogo.Value
                    ? query.Where(b => b.LogoUrl != null && b.LogoUrl != string.Empty)
                    : query.Where(b => b.LogoUrl == null || b.LogoUrl == string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(request.OriginCountry))
            {
                var country = request.OriginCountry;
                query = query.Where(b => b.OriginCountry == country);
            }

            if (request.ProductFilter == "With Products")
                query = query.Where(b => b.Products.Any());
            else if (request.ProductFilter == "Without Products")
                query = query.Where(b => !b.Products.Any());

            var sortAsc = request.SortAscending;
            query = (request.SortColumn ?? "Name") switch
            {
                "BrandCode" => sortAsc ? query.OrderBy(b => b.BrandCode) : query.OrderByDescending(b => b.BrandCode),
                "Description" => sortAsc ? query.OrderBy(b => b.Description) : query.OrderByDescending(b => b.Description),
                "OriginCompany" => sortAsc ? query.OrderBy(b => b.OriginCompany) : query.OrderByDescending(b => b.OriginCompany),
                "OriginCountry" => sortAsc ? query.OrderBy(b => b.OriginCountry) : query.OrderByDescending(b => b.OriginCountry),
                "FoundedYear" => sortAsc ? query.OrderBy(b => b.FoundedYear) : query.OrderByDescending(b => b.FoundedYear),
                "ProductCount" => sortAsc
                    ? query.OrderBy(b => b.Products.Count).ThenBy(b => b.Name)
                    : query.OrderByDescending(b => b.Products.Count).ThenBy(b => b.Name),
                "Status" => sortAsc ? query.OrderBy(b => b.Status) : query.OrderByDescending(b => b.Status),
                _ => sortAsc ? query.OrderBy(b => b.Name) : query.OrderByDescending(b => b.Name),
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Select(b => new { Brand = b, ProductCount = b.Products.Count })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = items
                .Select(x => new BrandDto(x.Brand.Id, x.Brand.Name, x.Brand.BrandCode, x.Brand.Description, x.ProductCount)
                {
                    LogoUrl = x.Brand.LogoUrl,
                    Status = x.Brand.Status,
                    CreatedAt = x.Brand.CreatedAt,
                    UpdatedAt = x.Brand.UpdatedAt,
                    OriginCompany = x.Brand.OriginCompany,
                    OriginCountry = x.Brand.OriginCountry,
                    FoundedYear = x.Brand.FoundedYear
                })
                .ToList();
            response.TotalCount = totalCount;
            response.CurrentPage = page;
            response.PageSize = pageSize;

            return response;
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
            await _cache.RemoveByPrefixAsync(ReferenceDataCache.Prefix);
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
            await _cache.RemoveByPrefixAsync(ReferenceDataCache.Prefix);
            return true;
        }
       
        public async Task<bool> DeleteBrandAsync(Guid id)
        {
            var brand = await _db.Brands.FindAsync(id) ?? throw new KeyNotFoundException("Brand not found");
            brand.IsDeleted = true;
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceDataCache.Prefix);
            return true;
        }

    }
}
