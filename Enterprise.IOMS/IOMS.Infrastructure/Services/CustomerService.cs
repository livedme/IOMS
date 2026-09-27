using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IOMS.Infrastructure.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CustomerService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
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
