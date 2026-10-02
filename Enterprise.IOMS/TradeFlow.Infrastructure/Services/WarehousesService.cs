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
    public class WarehousesService : IWarehousesService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ITenantCache _cache;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<WarehousesService> _logger;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public WarehousesService(
            ApplicationDbContext db,
            IMapper mapper,
            ITenantCache cache,
            ITenantProvider tenantProvider,
            ILogger<WarehousesService> logger,
            IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _db = db;
            _mapper = mapper;
            _cache = cache;
            _tenantProvider = tenantProvider;
            _logger = logger;
            _dbFactory = dbFactory;
        }

        public Task<List<WarehouseDto>> GetAllWarehousesAsync() =>
             _cache.GetOrCreateAsync(
                 $"{ReferenceDataCache.Prefix}warehouses",
                 ReferenceDataCache.Lifetime,
                 async ct =>
                 {
                     var warehouses = await _db.Warehouses.AsNoTracking()
                         .Where(w => w.IsActive)
                         .OrderBy(w => w.Name)
                         .ToListAsync(ct);
                     return _mapper.Map<List<WarehouseDto>>(warehouses);
                 });

        /// <summary>
        /// Active warehouses for pickers. Stocktakes.razor used to load these off the context as
        /// tracked entities and hand them straight to a MudBlazor select in its create dialog.
        /// </summary>
        public async Task<List<WarehouseDto>> GetActiveWarehousesAsync() =>
            await _db.Warehouses
                .AsNoTracking()
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .Select(w => new WarehouseDto(w.Id, w.Name, w.Code, w.Location, w.Address, w.IsActive))
                .ToListAsync();

        public async Task<WarehouseDto> GetWarehouseByIdAsync(Guid id)
        {
            var warehouse = await _db.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == id)
                ?? throw new KeyNotFoundException("Warehouse not found");

            return new WarehouseDto(warehouse.Id, warehouse.Name, warehouse.Code, warehouse.Location, warehouse.Address, warehouse.IsActive);
        }
        public async Task<PagedResultNew<WarehouseDto>> GetWarehousesPagedAsync(WarehousePagedRequest request)
        {
            // Own context for the whole read: the list can be re-entered while another query on the
            // scoped context is still in flight, and a DbContext cannot run two commands at once.
            await using var read = await _dbFactory.CreateDbContextAsync();
            var response = new PagedResultNew<WarehouseDto>();

            var page = Math.Max(0, request.CurrentPage);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var search = request.SearchTerm?.Trim();

            var query = read.Warehouses.AsNoTracking();
            if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
                query = query.Where(w => w.TenantId == request.TenantId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(w =>
                    EF.Functions.Like(w.Name, $"%{search}%") ||
                    EF.Functions.Like(w.Code, $"%{search}%") ||
                    EF.Functions.Like(w.Location, $"%{search}%") ||
                    EF.Functions.Like(w.Address, $"%{search}%"));
            }

            // Stats ignore the active filter so the tiles keep showing both buckets.
            var statRows = await query
                .GroupBy(w => w.IsActive)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            response.Stats["TotalCount"] = statRows.Sum(r => r.Count);
            response.Stats["ActiveCount"] = statRows.Where(r => r.Key).Sum(r => r.Count);
            response.Stats["InactiveCount"] = statRows.Where(r => !r.Key).Sum(r => r.Count);
            response.Stats["WithLocationCount"] = await query
                .CountAsync(w => w.Location != null && w.Location != string.Empty);
            response.Stats["UniqueCodeCount"] = await query.Select(w => w.Code).Distinct().CountAsync();

            if (request.IsActive.HasValue)
                query = query.Where(w => w.IsActive == request.IsActive.Value);

            var sortAsc = request.SortAscending;
            query = (request.SortColumn ?? "Name") switch
            {
                "Code" => sortAsc ? query.OrderBy(w => w.Code) : query.OrderByDescending(w => w.Code),
                "Location" => sortAsc ? query.OrderBy(w => w.Location) : query.OrderByDescending(w => w.Location),
                "Address" => sortAsc ? query.OrderBy(w => w.Address) : query.OrderByDescending(w => w.Address),
                "IsActive" => sortAsc ? query.OrderBy(w => w.IsActive) : query.OrderByDescending(w => w.IsActive),
                _ => sortAsc ? query.OrderBy(w => w.Name) : query.OrderByDescending(w => w.Name),
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip(page * pageSize).Take(pageSize).ToListAsync();

            response.Items = items
                .Select(w => new WarehouseDto(w.Id, w.Name, w.Code, w.Location, w.Address, w.IsActive))
                .ToList();
            response.TotalCount = totalCount;
            response.CurrentPage = page;
            response.PageSize = pageSize;

            return response;
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


        public async Task<Guid> CreateWarehouseAsync(CreateWarehouseDto dto)
        {
            var warehouse = new Warehouse { Name = dto.Name, Code = dto.Code, Location = dto.Location, Address = dto.Address };
            _db.Warehouses.Add(warehouse);
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceDataCache.Prefix);
            return warehouse.Id;
        }

        public async Task UpdateWarehouseAsync(Guid id, CreateWarehouseDto dto)
        {
            var warehouse = await _db.Warehouses.FindAsync(id) ?? throw new KeyNotFoundException("Warehouse not found");
            warehouse.Name = dto.Name;
            warehouse.Code = dto.Code;
            warehouse.Location = dto.Location;
            warehouse.Address = dto.Address;
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceDataCache.Prefix);
        }

      
        public async Task DeleteWarehouseAsync(Guid id)
        {
            var warehouse = await _db.Warehouses.FindAsync(id) ?? throw new KeyNotFoundException("Warehouse not found");
            warehouse.IsDeleted = true;
            await _db.SaveChangesAsync();
            await _cache.RemoveByPrefixAsync(ReferenceDataCache.Prefix);
        }

       
    }
}
