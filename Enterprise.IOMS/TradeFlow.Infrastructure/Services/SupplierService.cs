using AutoMapper;
using TradeFlow.Application.DTOs;
using TradeFlow.Application.Interfaces;
using TradeFlow.Domain.Entities;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TradeFlow.Infrastructure.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public SupplierService(ApplicationDbContext db, IMapper mapper, IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _db = db;
            _mapper = mapper;
            _dbFactory = dbFactory;
        }
        /// <summary>
        /// Simple paged supplier list for pickers and lookups. The Supplier grid uses the
        /// <see cref="GetSuppliersAsync(SupplierPagedRequest)"/> overload instead.
        /// </summary>
        /// <remarks>
        /// The count is awaited, not taken with a synchronous LINQ Count. The context is registered
        /// with EnableRetryOnFailure, so every operation is routed through a retrying execution
        /// strategy; a synchronous Count runs that strategy outside the async path and blocks a
        /// thread-pool thread inside an async method. Its CustomerService twin already does this
        /// correctly, and this overload had drifted away from it.
        /// </remarks>
        public async Task<PagedResult<SupplierDto>> GetSuppliersAsync(string? search = "", int page = 1, int pageSize = 100)
        {
            // Guard the inputs rather than trusting them. pageSize also feeds PagedResult.TotalPages,
            // which divides by it, so a zero here produced a divide-by-zero instead of an empty list.
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 1000);

            // AsNoTracking: this is a read-only list, so change tracking is pure overhead on every
            // row returned.
            IQueryable<Supplier> query = _db.Suppliers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(s => s.SupplierName.Contains(term)
                                         || (s.SupplierEmail != null && s.SupplierEmail.Contains(term)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(s => s.SupplierName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<SupplierDto>(_mapper.Map<List<SupplierDto>>(items), total, page, pageSize);
        }

        /// <summary>
        /// Active suppliers for pickers. Was duplicated across PaymentDialog and RfqDetail as a raw
        /// <c>DbContext.Suppliers</c> read that materialised tracked entities.
        /// </summary>
        public async Task<List<SupplierDto>> GetActiveSuppliersAsync() =>
            await _db.Suppliers
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.SupplierName)
                .Select(s => new SupplierDto
                {
                    Id = s.Id,
                    SupplierName = s.SupplierName,
                    SupplierEmail = s.SupplierEmail,
                    SupplierPhone = s.SupplierPhone,
                    ContactPersonName = s.ContactPersonName,
                    Address = s.Address,
                    City = s.City,
                    PaymentTerms = s.PaymentTerms,
                    LeadTimeDays = s.LeadTimeDays,
                    Rating = s.Rating,
                    IsActive = s.IsActive
                })
                .ToListAsync();

        /// <summary>
        /// Server-side paged supplier list used by the Supplier grid. Mirrors
        /// OrderService.GetSalesOrdersAsync: zero-based paging, filterable, sortable,
        /// and returns the tile counts in <see cref="PagedResultNew{T}.Stats"/>.
        /// </summary>
        public async Task<PagedResultNew<SupplierDto>> GetSuppliersAsync(SupplierPagedRequest request)
        {
            // Own context for the whole read: the list can be re-entered while another query on the
            // scoped context is still in flight, and a DbContext cannot run two commands at once.
            await using var read = await _dbFactory.CreateDbContextAsync();
            var response = new PagedResultNew<SupplierDto>();

            var page = Math.Max(0, request.CurrentPage);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var search = request.SearchTerm?.Trim();
            var hasSearch = !string.IsNullOrWhiteSpace(search);

            IQueryable<Supplier> query = read.Suppliers.AsNoTracking();

            if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
                query = query.Where(s => s.TenantId == request.TenantId.Value);

            // Stats ignore the status/city filters so the tiles keep showing every
            // bucket while one of those filters is active.
            var statsSource = ApplySupplierFilters(query, request, search, hasSearch);
            var statRows = await statsSource
                .GroupBy(s => s.IsActive)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            response.Stats["TotalCount"] = statRows.Sum(r => r.Count);
            response.Stats["ActiveCount"] = statRows.Where(r => r.Key).Sum(r => r.Count);
            response.Stats["InactiveCount"] = statRows.Where(r => !r.Key).Sum(r => r.Count);
            response.Stats["AverageRating"] = (int)Math.Round(
                await statsSource.AverageAsync(s => (decimal?)s.Rating) ?? 0m);
            response.Stats["AverageLeadTime"] = (int)Math.Round(
                await statsSource.AverageAsync(s => (decimal?)s.LeadTimeDays) ?? 0m);
            response.Stats["WithEmailCount"] = await statsSource
                .CountAsync(s => s.SupplierEmail != null && s.SupplierEmail != string.Empty);
            response.Stats["CityCount"] = await statsSource
                .Where(s => s.City != null && s.City != string.Empty)
                .Select(s => s.City)
                .Distinct()
                .CountAsync();

            query = ApplySupplierFilters(query, request, search, hasSearch);

            var sortAsc = request.SortAscending;
            query = (request.SortColumn ?? "SupplierName") switch
            {
                "Email" => sortAsc ? query.OrderBy(s => s.SupplierEmail) : query.OrderByDescending(s => s.SupplierEmail),
                "Phone" => sortAsc ? query.OrderBy(s => s.SupplierPhone) : query.OrderByDescending(s => s.SupplierPhone),
                "ContactPerson" => sortAsc ? query.OrderBy(s => s.ContactPersonName) : query.OrderByDescending(s => s.ContactPersonName),
                "City" => sortAsc ? query.OrderBy(s => s.City) : query.OrderByDescending(s => s.City),
                "PaymentTerms" => sortAsc ? query.OrderBy(s => s.PaymentTerms) : query.OrderByDescending(s => s.PaymentTerms),
                "LeadTimeDays" => sortAsc ? query.OrderBy(s => s.LeadTimeDays) : query.OrderByDescending(s => s.LeadTimeDays),
                "Rating" => sortAsc ? query.OrderBy(s => s.Rating) : query.OrderByDescending(s => s.Rating),
                "IsActive" => sortAsc ? query.OrderBy(s => s.IsActive) : query.OrderByDescending(s => s.IsActive),
                _ => sortAsc ? query.OrderBy(s => s.SupplierName) : query.OrderByDescending(s => s.SupplierName),
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = _mapper.Map<List<SupplierDto>>(items);
            response.TotalCount = totalCount;
            response.CurrentPage = page;
            response.PageSize = pageSize;

            return response;
        }

        public async Task<List<string>> GetSupplierCitiesAsync()
        {
            return await _db.Suppliers.AsNoTracking()
                .Where(s => s.City != null && s.City != string.Empty)
                .Select(s => s.City!)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
        }

        private static IQueryable<Supplier> ApplySupplierFilters(
            IQueryable<Supplier> query, SupplierPagedRequest request, string? search, bool hasSearch)
        {
            if (hasSearch)
            {
                query = query.Where(s =>
                    EF.Functions.Like(s.SupplierName, $"%{search}%") ||
                    EF.Functions.Like(s.SupplierEmail, $"%{search}%") ||
                    EF.Functions.Like(s.SupplierPhone, $"%{search}%") ||
                    EF.Functions.Like(s.ContactPersonName, $"%{search}%") ||
                    EF.Functions.Like(s.City, $"%{search}%"));
            }

            if (request.IsActive.HasValue)
                query = query.Where(s => s.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.City))
                query = query.Where(s => s.City == request.City);

            return query;
        }

        public async Task<SupplierDto?> GetSupplierByIdAsync(Guid id)
        {
            var supplier = await _db.Suppliers.FindAsync(id);
            return supplier == null ? null : _mapper.Map<SupplierDto>(supplier);
        }

        public async Task<Guid> CreateSupplierAsync(SupplierDto dto)
        {
            var supplier = new Supplier
            {
                SupplierName = dto.SupplierName,
                SupplierEmail = dto.SupplierEmail,
                SupplierPhone = dto.SupplierPhone,
                ContactPersonName = dto.ContactPersonName,
                ContactPersonEmail = dto.ContactPersonEmail,
                ContactPersonPhone = dto.ContactPersonPhone,
                Address = dto.Address,
                City = dto.City,
                Zila = dto.Zila,
                State = dto.State,
                Country = dto.Country,
                PostalCode = dto.PostalCode,
                PaymentTerms = dto.PaymentTerms,
                DefaultCurrencyId = dto.DefaultCurrencyId,
                LeadTimeDays = dto.LeadTimeDays
            };

            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();
            return supplier.Id;
        }

        public async Task UpdateSupplierAsync(Guid id, SupplierDto dto)
        {
            var supplier = await _db.Suppliers.FindAsync(id) ?? throw new KeyNotFoundException("Supplier not found");
            supplier.SupplierName = dto.SupplierName; supplier.SupplierEmail = dto.SupplierEmail; supplier.SupplierPhone = dto.SupplierPhone;
            supplier.ContactPersonName = dto.ContactPersonName; supplier.ContactPersonEmail = dto.ContactPersonEmail; supplier.ContactPersonPhone = dto.ContactPersonPhone;
            supplier.Address = dto.Address; supplier.City = dto.City; supplier.State = dto.State; supplier.Zila = dto.Zila;
            supplier.Country = dto.Country; supplier.PostalCode = dto.PostalCode;
            supplier.PaymentTerms = dto.PaymentTerms; supplier.LeadTimeDays = dto.LeadTimeDays;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteSupplierAsync(Guid id)
        {
            var supplier = await _db.Suppliers.FindAsync(id) ?? throw new KeyNotFoundException("Supplier not found");
            supplier.IsDeleted = true;
            await _db.SaveChangesAsync();
        }
    }
}
