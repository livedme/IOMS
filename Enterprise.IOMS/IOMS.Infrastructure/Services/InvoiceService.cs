using AutoMapper;
using IOMS.Application.DTOs;
using IOMS.Application.Interfaces;
using IOMS.Domain.Entities;
using IOMS.Domain.Enums;
using IOMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IOMS.Infrastructure.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ISharedNumberGenerator _numberGen;

        public InvoiceService(ApplicationDbContext db, IMapper mapper, ISharedNumberGenerator numberGen)
        {
            _db = db;
            _mapper = mapper;
            _numberGen = numberGen;
        }

        public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(string? search, InvoiceStatus? status, InvoiceType? type, int page, int pageSize)
        {
            var query = _db.Invoices.Include(i => i.Customer).Include(i => i.Supplier).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.InvoiceNumber.Contains(search));
            if (status.HasValue) query = query.Where(i => i.Status == status.Value);
            if (type.HasValue) query = query.Where(i => i.InvoiceType == type.Value);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(i => i.InvoiceDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<InvoiceDto>(_mapper.Map<List<InvoiceDto>>(items), total, page, pageSize);
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id)
        {
            var invoice = await _db.Invoices.Include(i => i.Customer).Include(i => i.Supplier).FirstOrDefaultAsync(i => i.Id == id);
            return invoice == null ? null : _mapper.Map<InvoiceDto>(invoice);
        }

        public async Task<Guid> CreateInvoiceAsync(CreateInvoiceDto dto)
        {
            var invoice = new Invoice
            {
                InvoiceNumber = _numberGen.Generate("INV"),
                InvoiceType = dto.InvoiceType,
                SalesOrderId = dto.SalesOrderId,
                PurchaseOrderId = dto.PurchaseOrderId,
                CustomerId = dto.CustomerId,
                SupplierId = dto.SupplierId,
                DueDate = dto.DueDate,
                Notes = dto.Notes
            };

            if (dto.SalesOrderId.HasValue)
            {
                var so = await _db.SalesOrders.FindAsync(dto.SalesOrderId.Value);
                if (so != null) { invoice.SubTotal = so.SubTotal; invoice.TaxAmount = so.TaxAmount; invoice.TotalAmount = so.TotalAmount; invoice.CustomerId = so.CustomerId; }
            }
            else if (dto.PurchaseOrderId.HasValue)
            {
                var po = await _db.PurchaseOrders.FindAsync(dto.PurchaseOrderId.Value);
                if (po != null) { invoice.SubTotal = po.SubTotal; invoice.TaxAmount = po.TaxAmount; invoice.TotalAmount = po.TotalAmount; invoice.SupplierId = po.SupplierId; }
            }

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            return invoice.Id;
        }

        public async Task<Guid> GenerateInvoiceFromSalesOrderAsync(Guid salesOrderId)
        {
            var so = await _db.SalesOrders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == salesOrderId)
                ?? throw new KeyNotFoundException("Sales order not found");
            return await CreateInvoiceAsync(new CreateInvoiceDto(InvoiceType.Sales, salesOrderId, null, so.CustomerId, null, DateTime.UtcNow.AddDays(30), null));
        }

        public async Task<Guid> GenerateInvoiceFromPurchaseOrderAsync(Guid purchaseOrderId)
        {
            var po = await _db.PurchaseOrders.Include(o => o.Supplier).FirstOrDefaultAsync(o => o.Id == purchaseOrderId)
                ?? throw new KeyNotFoundException("Purchase order not found");
            return await CreateInvoiceAsync(new CreateInvoiceDto(InvoiceType.Purchase, null, purchaseOrderId, null, po.SupplierId, DateTime.UtcNow.AddDays(30), null));
        }
    }

}
