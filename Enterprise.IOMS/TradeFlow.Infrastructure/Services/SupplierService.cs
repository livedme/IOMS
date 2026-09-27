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

        public SupplierService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }
        public async Task<PagedResult<SupplierDto>> GetSuppliersAsync(string? search = "", int page = 1, int pageSize = 100)
        {
            var query = _db.Suppliers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.SupplierName.Contains(search) || (s.SupplierEmail != null && s.SupplierEmail.Contains(search)));
            var total = query.Count(); 
            var items = query.OrderBy(s => s.SupplierName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<SupplierDto>(_mapper.Map<List<SupplierDto>>(items), total, page, pageSize);
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
