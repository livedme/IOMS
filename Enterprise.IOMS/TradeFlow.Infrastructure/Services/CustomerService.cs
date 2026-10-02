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
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public CustomerService(ApplicationDbContext db, IMapper mapper, IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _db = db;
            _mapper = mapper;
            _dbFactory = dbFactory;
        }

        public async Task<PagedResult<CustomerDto>> GetCustomersAsync(string? search, int page = 1, int pageSize = 100)
        {
            var query = _db.Customers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.CustomerName.Contains(search) || (c.CustomerEmail != null && c.CustomerEmail.Contains(search)));
            var total = await query.CountAsync();
            var items = await query.OrderBy(c => c.CustomerName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<CustomerDto>(_mapper.Map<List<CustomerDto>>(items), total, page, pageSize);
        }

        /// <summary>
        /// Active customers for pickers. Was duplicated across seven components as a raw
        /// <c>DbContext.Customers</c> read, each with a slightly different projection.
        /// </summary>
        public async Task<List<CustomerDto>> GetActiveCustomersAsync() =>
            await _db.Customers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CustomerName)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    CustomerName = c.CustomerName,
                    CustomerEmail = c.CustomerEmail,
                    CustomerPhone = c.CustomerPhone,
                    ContactPersonName = c.ContactPersonName,
                    Address = c.Address,
                    City = c.City,
                    CreditLimit = c.CreditLimit,
                    PaymentTerms = c.PaymentTerms,
                    IsActive = c.IsActive
                })
                .ToListAsync();

        /// <summary>
        /// Server-side paged customer list used by the Customer grid. Mirrors
        /// OrderService.GetSalesOrdersAsync: zero-based paging, filterable, sortable,
        /// and returns the tile counts in <see cref="PagedResultNew{T}.Stats"/>.
        /// </summary>
        public async Task<PagedResultNew<CustomerDto>> GetCustomersAsync(CustomerPagedRequest request)
        {
            // Own context for the whole read: the list can be re-entered while another query on the
            // scoped context is still in flight, and a DbContext cannot run two commands at once.
            await using var read = await _dbFactory.CreateDbContextAsync();
            var response = new PagedResultNew<CustomerDto>();

            var page = Math.Max(0, request.CurrentPage);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var search = request.SearchTerm?.Trim();
            var hasSearch = !string.IsNullOrWhiteSpace(search);

            IQueryable<Customer> query = read.Customers.AsNoTracking();

            if (request.TenantId.HasValue && request.TenantId != Guid.Empty)
                query = query.Where(c => c.TenantId == request.TenantId.Value);

            // Stats ignore the status/city filters so the tiles keep showing every bucket
            // while one of those filters is active — same rule as the sales grid.
            var statsSource = ApplyCustomerFilters(query, request, search, hasSearch);
            var statRows = await statsSource
                .GroupBy(c => new { c.IsActive, c.IsTaxExempt })
                .Select(g => new { g.Key.IsActive, g.Key.IsTaxExempt, Count = g.Count() })
                .ToListAsync();

            response.Stats["TotalCount"] = statRows.Sum(r => r.Count);
            response.Stats["ActiveCount"] = statRows.Where(r => r.IsActive).Sum(r => r.Count);
            response.Stats["InactiveCount"] = statRows.Where(r => !r.IsActive).Sum(r => r.Count);
            response.Stats["TaxExemptCount"] = statRows.Where(r => r.IsTaxExempt).Sum(r => r.Count);
            response.Stats["TotalCreditLimit"] = (int)Math.Round(await statsSource.SumAsync(c => (decimal?)c.CreditLimit) ?? 0m);
            response.Stats["CityCount"] = await statsSource
                .Where(c => c.City != null && c.City != string.Empty)
                .Select(c => c.City)
                .Distinct()
                .CountAsync();

            query = ApplyCustomerFilters(query, request, search, hasSearch);

            var sortAsc = request.SortAscending;
            query = (request.SortColumn ?? "CustomerName") switch
            {
                "Email" => sortAsc ? query.OrderBy(c => c.CustomerEmail) : query.OrderByDescending(c => c.CustomerEmail),
                "Phone" => sortAsc ? query.OrderBy(c => c.CustomerPhone) : query.OrderByDescending(c => c.CustomerPhone),
                "ContactPerson" => sortAsc ? query.OrderBy(c => c.ContactPersonName) : query.OrderByDescending(c => c.ContactPersonName),
                "City" => sortAsc ? query.OrderBy(c => c.City) : query.OrderByDescending(c => c.City),
                "CreditLimit" => sortAsc ? query.OrderBy(c => c.CreditLimit) : query.OrderByDescending(c => c.CreditLimit),
                "PaymentTerms" => sortAsc ? query.OrderBy(c => c.PaymentTerms) : query.OrderByDescending(c => c.PaymentTerms),
                "IsActive" => sortAsc ? query.OrderBy(c => c.IsActive) : query.OrderByDescending(c => c.IsActive),
                _ => sortAsc ? query.OrderBy(c => c.CustomerName) : query.OrderByDescending(c => c.CustomerName),
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.Items = _mapper.Map<List<CustomerDto>>(items);
            response.TotalCount = totalCount;
            response.CurrentPage = page;
            response.PageSize = pageSize;

            return response;
        }

        public async Task<List<string>> GetCustomerCitiesAsync()
        {
            return await _db.Customers.AsNoTracking()
                .Where(c => c.City != null && c.City != string.Empty)
                .Select(c => c.City!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        private static IQueryable<Customer> ApplyCustomerFilters(
            IQueryable<Customer> query, CustomerPagedRequest request, string? search, bool hasSearch)
        {
            if (hasSearch)
            {
                query = query.Where(c =>
                    EF.Functions.Like(c.CustomerName, $"%{search}%") ||
                    EF.Functions.Like(c.CustomerEmail, $"%{search}%") ||
                    EF.Functions.Like(c.CustomerPhone, $"%{search}%") ||
                    EF.Functions.Like(c.ContactPersonName, $"%{search}%") ||
                    EF.Functions.Like(c.City, $"%{search}%"));
            }

            if (request.IsActive.HasValue)
                query = query.Where(c => c.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.City))
                query = query.Where(c => c.City == request.City);

            return query;
        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
        {
            var customer = await _db.Customers.FindAsync(id);
            return customer == null ? null : _mapper.Map<CustomerDto>(customer);
        }

        public async Task<Guid> CreateCustomerAsync(CustomerDto dto)
        {
            var customer = new Customer
            {
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                CustomerPhone = dto.CustomerPhone,
                ContactPersonEmail = dto.ContactPersonEmail,
                ContactPersonPhone = dto.ContactPersonPhone,
                ContactPersonName = dto.ContactPersonName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Country = dto.Country,
                PostalCode = dto.PostalCode,
                CreditLimit = dto.CreditLimit,
                PaymentTerms = dto.PaymentTerms,
                TaxJurisdictionId = dto.TaxJurisdictionId,
                DefaultPriceListId = dto.DefaultPriceListId,
                DefaultCurrencyId = dto.DefaultCurrencyId,
                IsTaxExempt = dto.IsTaxExempt,
                IsActive = dto.IsActive
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
            return customer.Id;
        }

        public async Task UpdateCustomerAsync(Guid id, CustomerDto dto)
        {
            var customer = await _db.Customers.FindAsync(id) ?? throw new KeyNotFoundException("Customer not found");
            customer.CustomerName = dto.CustomerName; customer.CustomerEmail = dto.CustomerEmail; customer.CustomerPhone = dto.CustomerPhone;
            customer.ContactPersonName = dto.ContactPersonName; customer.ContactPersonEmail = dto.ContactPersonEmail; customer.ContactPersonPhone = dto.ContactPersonPhone;
            customer.Address = dto.Address; customer.City = dto.City; customer.State = dto.State;
            customer.Country = dto.Country; customer.PostalCode = dto.PostalCode;
            customer.CreditLimit = dto.CreditLimit; customer.PaymentTerms = dto.PaymentTerms;
            customer.IsTaxExempt = dto.IsTaxExempt;
            customer.IsActive = dto.IsActive;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteCustomerAsync(Guid id)
        {
            var customer = await _db.Customers.FindAsync(id) ?? throw new KeyNotFoundException("Customer not found");
            customer.IsDeleted = true;
            await _db.SaveChangesAsync();
        }

    }
}
